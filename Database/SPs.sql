/*******************************************
 * 2.1) usp_EnqueueOrArchiveIfDuplicate
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_EnqueueOrArchiveIfDuplicate
  @CustId  		INT,
  @ChatId      		NVARCHAR(50),
  @BotKey      		NVARCHAR(100),
  @MessageText 		NVARCHAR(MAX),
  @PhoneNumber      NVARCHAR(20),
  @MsgType     		NVARCHAR(10), 
  @CampaignId  		NVARCHAR(50), -- Empty String if not required
  @CampDescription 	NVARCHAR(512), -- Empty String if not required
  @Priority    		SMALLINT,
  @ScheduledSendDateTime DATETIME = NULL,  -- Auto inserted in case of one message
  @IsSystemApproved BIT
--@Paused = 0 always zero and it will change when the portal change them
AS
BEGIN
  SET NOCOUNT ON;
  SET @ScheduledSendDateTime = ISNULL(@ScheduledSendDateTime, GETDATE());

  DECLARE 
    @hashedMsg   BINARY(32) = HASHBYTES(
             'SHA2_256',
             @ChatId + N'|' + @BotKey + N'|' + @MessageText
           );
    -- If caller omitted it, fill it with GETDATE()

  -- always enqueue; trigger will handle RecentMessages & archiving
 INSERT INTO dbo.ReadyTable
    (ChatId
    ,CustId
    ,BotKey
    ,PhoneNumber
    ,MessageText
    ,MsgType
    ,ReceivedDateTime
    ,ScheduledSendDateTime     -- ← add this
    ,MessageHash
    ,Priority
    ,CampaignId
    ,CampDescription
    ,IsSystemApproved
    ,Paused)
  VALUES
    (@ChatId
    ,@CustId
    ,@BotKey
    ,@PhoneNumber
    ,@MessageText
    ,@MsgType
    ,GETDATE()
    ,@ScheduledSendDateTime   -- ← use your variable here
    ,@hashedMsg
    ,@Priority
    ,@CampaignId
    ,@CampDescription
    ,@IsSystemApproved
    ,0);

  SELECT SCOPE_IDENTITY() AS NewId;
END;
GO



/*******************************************
 * 2.4) usp_GetCustomerByUsername
 *******************************************/
ALTER PROCEDURE [dbo].[usp_GetCustomerByUsername]
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustId, UserName, Password, RequireSystemApprove,
                     RequireAdminApprove, IsActive, IsBlocked, IsTelegramActive
                     FROM A2A_iMessaging.dbo.Table_UserSMSProfile
                     WHERE UserName = @Username
END;
GO


/*******************************************
 * 2.5) usp_GetTelegramUser
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_GetTelegramUser
  @BotKey       NVARCHAR(100),
  @PhoneNumber  NVARCHAR(20)
AS
BEGIN
  SET NOCOUNT ON;

  -- assign the output param directly
  SELECT *
  FROM 
    dbo.BotChatMapping WITH (INDEX(PK_BotChatMapping))
  WHERE 
    PhoneNumber = @PhoneNumber
    AND BotKey   = @BotKey;
END;
GO


/*******************************************
 * 2.6) usp_GetChatIdsForPhones
 *******************************************/
-- Proc: returns (PhoneNumber, ChatId) for a given BotKey
CREATE OR ALTER PROCEDURE dbo.usp_GetChatIdsForPhones
  @BotKey       NVARCHAR(100),
  @PhoneNumbers dbo.PhoneList READONLY
AS
BEGIN
  SET NOCOUNT ON;

  SELECT p.PhoneNumber,
         u.ChatId
  FROM @PhoneNumbers AS p
  LEFT JOIN dbo.BotChatMapping AS u
    ON u.BotKey = @BotKey
   AND u.PhoneNumber = p.PhoneNumber;
END
GO


/*******************************************
 * 2.7) usp_AddBatchFile to add the batch data into DB
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_AddBatchFile
    @CustId               INT,
    @BotKey               NVARCHAR(100),
    @MsgText              NVARCHAR(MAX) = NULL,
    @MsgType              NVARCHAR(10),
    @CampaignId           NVARCHAR(50),
    @CampDesc             NVARCHAR(256) = NULL,
    @Priority             SMALLINT,           -- table uses SMALLINT
    @IsSystemApproved     BIT,
    @IsAdminApproved      BIT,
    @ScheduledSendDateTime DATETIME2 = NULL,  -- if NULL => GETDATE()
    @FilePath             NVARCHAR(260),
    @FileType             NVARCHAR(16),
    @IsProcessed          BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = GETDATE();

    INSERT INTO dbo.TelegramFiles
    (
        CustID,
        BotKey,
        MsgText,
        MsgType,
        Priority,
        FilePath,
        FileType,
        CampaignID,
        CampDesc,
        ScheduledSendDateTime,
        CreationDate,
        IsSystemApproved,
        IsAdminApproved,
        IsProcessed
    )
    VALUES
    (
        @CustId,
        @BotKey,
        @MsgText,
        @MsgType,
        @Priority,
        @FilePath,
        @FileType,
        @CampaignId,
        @CampDesc,
        ISNULL(@ScheduledSendDateTime, @Now),
        @Now,
        @IsSystemApproved,
        @IsAdminApproved,
        @IsProcessed
    );
END
GO

/*******************************************
 * 2.8) usp_ReadyTable_BulkEnqueue
 *******************************************/

CREATE OR ALTER PROCEDURE dbo.usp_ReadyTable_BulkEnqueue
  @Batch dbo.TelegramMessage_Tvp READONLY
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @now DATETIME = GETDATE();


  INSERT INTO dbo.ReadyTable
  (
    CustId,
    ChatId, 
    BotKey, 
    PhoneNumber, 
    MessageText, 
    MsgType,
    ReceivedDateTime, 
    ScheduledSendDateTime, 
    MessageHash,
    Priority, 
    CampaignId, 
    CampDescription, 
    IsSystemApproved, 
    Paused
  )
  SELECT
    b.CustomerId,
    NULLIF(b.ChatId, N''),
    b.BotKey,
    b.PhoneNumber,
    b.MessageText,
    b.MessageType,
    @now,
    ISNULL(b.ScheduledSendDateTime, @now),
    HASHBYTES(
      'SHA2_256',
      CONCAT(ISNULL(b.ChatId, N''), N'|', b.BotKey, N'|', b.MessageText)
    ),
    b.Priority,
    NULLIF(b.CampaignId, N''),
    NULLIF(b.CampDescription, N''),
    b.IsSystemApproved,
    0
  FROM @Batch AS b;
END
GO

/*******************************************
 * 2.9) usp_GetBulkMessageByCampaignId
 *******************************************/

CREATE OR ALTER PROCEDURE dbo.usp_GetBulkMessageByCampaignId
    @CampaignId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CustID,
        BotKey,
        MsgText,
        MsgType,
        Priority,
        FilePath,
        FileType,
        CampaignID,
        CampDesc,
        ScheduledSendDateTime,
        CreationDate,
        isSystemApproved,
        isAdminApproved,
        IsProcessed
    FROM dbo.TelegramFiles
    WHERE CampaignID = @CampaignId AND IsProcessed = 0;
END;
GO

/*******************************************
 * 2.10) usp_ArchiveTelegramFileByCampaignId
 *******************************************/

CREATE OR ALTER PROCEDURE dbo.usp_ArchiveTelegramFileByCampaignId
    @CampaignID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into TelegramSentFiles the matching campaign
        INSERT INTO dbo.TelegramSentFiles (
            CustID, BotKey, MsgText, MsgType, Priority,
            FilePath, FileType, CampaignID, CampDesc,
            ScheduledSendDateTime, CreationDate,
            isSystemApproved, isAdminApproved, IsProcessed
        )
        SELECT
            CustID, BotKey, MsgText, MsgType, Priority,
            FilePath, FileType, CampaignID, CampDesc,
            ScheduledSendDateTime, CreationDate,
            isSystemApproved, isAdminApproved, IsProcessed
        FROM dbo.TelegramFiles
        WHERE CampaignID = @CampaignID;

        -- Delete from TelegramFiles after successful insert
        DELETE FROM dbo.TelegramFiles
        WHERE CampaignID = @CampaignID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

