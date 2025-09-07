using System.Text.Json;
using A2ASerilog;
using Mapster;
using Telegram.Listener.Domain.Entities;
using Telegram.Listener.Domain.Enums;
using Telegram.Listener.Domain.Interfaces.Application;
using Telegram.Listener.Domain.Interfaces.Infrastructure.Repositories;

namespace Telegram.Listener.Application.Services;

public class QueuedMessagesService(
    IDropFolderRepository dropFolderRepository,
    IMessageRepository messageRepository)
    : IQueuedMessagesService
{
    private readonly IDropFolderRepository _dropFolderRepository = dropFolderRepository;
    private readonly IMessageRepository _messagesRepository = messageRepository;

    public async Task<bool> ProcessQueuedMessagesAsync(CancellationToken cancellationToken = default)
    {
        // Get the Directory that have json files.
        // 1) discover files (assume IEnumerable<string> of full paths)
        LockedBulkJsonFile? file = await _dropFolderRepository.GetLockedJsonFilesAsync(cancellationToken);

        if (file is null)
        {
            LoggerService.Info("No files found to process.");
            return false; // nothing to do
        }

        try
        {
            await ProcessOneFileAsync(file, cancellationToken); // parses + maps + persists
            return true; // did work
        }
        catch (OperationCanceledException) { throw; }
        catch (JsonException jex) { LoggerService.Error("JSON error {File} " + jex.Message, file.FileName); }
        catch (Exception ex) { LoggerService.Error("Processing error for file {File}: {ErrorMessage}", file.FileName, ex.Message); }
        finally
        {
            await file.LockHandle.DisposeAsync(); // release OS lock
            await _dropFolderRepository.ArchiveFileAsync(file.FullPath, cancellationToken);
            // File name here is the campaignId
            await _messagesRepository.ArchiveDbFileAsync(file.FileName, cancellationToken);
        }
        return false; // error occurred, but we did work (found a file)
    }

    /// <summary>
    /// Processes a single locked bulk JSON file: reads metadata to interpret the JSON as either a batch or campaign payload,
    /// converts the contained records into TelegramMessage instances, and persists them via the messages repository.
    /// </summary>
    /// <remarks>
    /// If metadata is missing, the file type is invalid, the JSON is empty/invalid, or any non-recoverable error occurs,
    /// the method returns without persisting messages (the caller is responsible for releasing locks and archiving the file).
    /// </remarks>
    /// <param name="file">The locked bulk JSON file to process (provides FileName and Content).</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel processing.</param>
    /// <returns>A task that completes when processing finishes.</returns>
    private async Task ProcessOneFileAsync(LockedBulkJsonFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            // 3) campaign/batch metadata (tells us how to interpret the JSON)
            BulkMessage? bulkMessage = await _messagesRepository.GetBulkMessageByCampaignIdAsync(file.FileName, cancellationToken);
            if (bulkMessage is null)
            {
                LoggerService.Warning("No metadata found for file {File}. Archiving as error.", file.FileName);
                return;
            }

            // Normalize/parse file type once
            if (!Enum.TryParse(bulkMessage.FileType, ignoreCase: true, out FileTypeEnum fileType))
            {
                LoggerService.Warning("Invalid file type '{Type}' in metadata for file {File}. Archiving as error.", bulkMessage.FileType, file.FileName);
                return;
            }

            List<TelegramMessage> messages;

            switch (fileType)
            {
                case FileTypeEnum.Batch:
                    {
                        LoggerService.Info("Processing batch file {File} (CustId {CustId})", file.FileName, bulkMessage.CustomerId);

                        List<BatchMessages>? batchItems = JsonSerializer.Deserialize<List<BatchMessages>>(file.Content);
                        if (batchItems is null || batchItems.Count == 0)
                        {
                            LoggerService.Warning("No messages found in batch file {File}. Archiving as error.", file.FileName);
                            return;
                        }

                        LoggerService.Info("Found {Count} messages in batch file {File}", batchItems.Count, file.FileName);

                        messages = new List<TelegramMessage>(batchItems.Count);
                        foreach (BatchMessages item in batchItems)
                            messages.Add((bulkMessage, item).Adapt<TelegramMessage>());

                        break;
                    }

                case FileTypeEnum.Campaign:
                    {
                        LoggerService.Info("Processing campaign file {File} (CampaignId {CampaignId})", file.FileName, bulkMessage.CampaignId);

                        List<CampaignMessage>? campaignItems = JsonSerializer.Deserialize<List<CampaignMessage>>(file.Content);
                        if (campaignItems is null || campaignItems.Count == 0)
                        {
                            LoggerService.Warning("No messages found in campaign file {File}. Archiving as error.", file.FileName);
                            return;
                        }

                        LoggerService.Info("Found {Count} messages in campaign file {File}", campaignItems.Count, file.FileName);

                        messages = new List<TelegramMessage>(campaignItems.Count);
                        foreach (CampaignMessage item in campaignItems)
                            messages.Add((bulkMessage, item).Adapt<TelegramMessage>());

                        break;
                    }

                default:
                    LoggerService.Warning("Unknown file type {Type} for file {File}. Archiving as error.", fileType, file.FileName);
                    return;
            }

            await _messagesRepository.AddBatchAsync(messages, cancellationToken);

            LoggerService.Info("Successfully processed file {File}", file.FileName);
        }
        catch (JsonException jex)
        {
            LoggerService.Error("JSON parse error in file {File} " + jex.Message, file.FileName);
            return; // skip to next file
        }
        catch (OperationCanceledException)
        {
            LoggerService.Warning("Processing canceled while handling file {File}", file.FileName);
            return; // let outer loop handle cancellation
        }
        catch (Exception ex)
        {
            LoggerService.Error("Error deleting the file: " + ex.Message, file.FileName);
            LoggerService.Error("Error processing file {File} " + ex.Message, file.FileName);
            // Consider moving the file to a 'failed' archive to avoid hot-looping on the same error.
            return; // skip to next file
        }
    }
}
