/*******************************************
 * 2.1) usp_EnqueueOrArchiveIfDuplicate
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_EnqueueOrArchiveIfDuplicate
  @CustomerId  		INT,
  @ChatId      		NVARCHAR(50),
  @BotId      		INT,
  @MessageText 		NVARCHAR(MAX),
  @PhoneNumber      NVARCHAR(32),
  @MsgType     		NVARCHAR(10), 
  @CampaignId  		NVARCHAR(50), -- Empty String if not required
  @CampDescription 	NVARCHAR(512), -- Empty String if not required
  @Priority    		SMALLINT,
  @ScheduledSendDateTime DATETIME2 = NULL,  -- Auto inserted in case of one message
  @IsSystemApproved BIT
--@Paused = 0 always zero and it will change when the portal change them
AS
BEGIN
  SET NOCOUNT ON;
  SET @ScheduledSendDateTime = ISNULL(@ScheduledSendDateTime, GETDATE());

  DECLARE 
    @hashedMsg   BINARY(32) = HASHBYTES(
             'SHA2_256',
              CONCAT(ISNULL(@ChatId, N''), N'|', @BotId, N'|', ISNULL(@MessageText, N'')) 
          );
    -- If caller omitted it, fill it with GETDATE()

  -- always enqueue; trigger will handle RecentMessages & archiving
 INSERT INTO dbo.ReadyTable
    (ChatId
    ,CustomerId
    ,BotId
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
    ,@CustomerId
    ,@BotId
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
CREATE OR ALTER PROCEDURE [dbo].[usp_GetCustomerByUsername]
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustId, 
            UserName, 
            Password, 
            RequireSystemApprove,
            RequireAdminApprove, 
            IsActive, 
            IsBlocked, 
            IsTelegramActive
    FROM A2A_iMessaging.dbo.Table_UserSMSProfile
    WHERE UserName = @Username
END;
GO


/*******************************************
 * 2.5) usp_GetTelegramUser   (NEW)
 * Returns the TelegramUserChats row for a given BotId
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_GetTelegramUser
  @BotId                 INT,
  @PhoneNumber           NVARCHAR(32)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT * FROM dbo.TelegramUserChats
  WHERE   BotId     = @BotId
    AND   PhoneNumber = @PhoneNumber;
END
GO


/*******************************************
 * 2.6) usp_GetChatIdsForPhones   (NEW)
 * Input:  @BotId, @PhoneNumbers TVP (expects E.164 in PhoneNumber)
 * Output: one row per requested phone with ChatId (NULL if not found)
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_GetChatIdsForPhones
  @BotId        INT,
  @PhoneNumbers dbo.PhoneList READONLY
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
      p.PhoneNumber,
      u.ChatId
  FROM @PhoneNumbers AS p
  LEFT JOIN dbo.TelegramUserChats AS u
    ON u.BotId      = @BotId
   AND u.PhoneNumber  = p.PhoneNumber
   AND u.IsActive = 1;
END
GO


/*******************************************
 * 2.7) usp_AddBatchFile to add the batch data into DB
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_AddBatchFile
    @CustomerId           INT,
    @BotId                INT,
    @MsgText              NVARCHAR(MAX) = NULL,
    @MsgType              NVARCHAR(10),
    @CampaignId           NVARCHAR(50),
    @CampDescription             NVARCHAR(256) = NULL,
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
        CustomerId,
        BotId,
        MsgText,
        MsgType,
        Priority,
        FilePath,
        FileType,
        CampaignID,
        CampDescription,
        ScheduledSendDateTime,
        CreationDate,
        IsSystemApproved,
        IsAdminApproved,
        IsProcessed
    )
    VALUES
    (
        @CustomerId,
        @BotId,
        @MsgText,
        @MsgType,
        @Priority,
        @FilePath,
        @FileType,
        @CampaignId,
        @CampDescription,
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

  DECLARE @now DATETIME2 = GETDATE();


  INSERT INTO dbo.ReadyTable
  (
    CustomerId,
    ChatId, 
    BotId, 
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
    b.BotId,
    b.PhoneNumber,
    b.MessageText,
    b.MessageType,
    @now,
    ISNULL(b.ScheduledSendDateTime, @now),
    HASHBYTES(
      'SHA2_256',
      CONCAT(ISNULL(b.ChatId, N''), N'|', b.BotId, N'|', b.MessageText)
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

    SELECT *
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
            CustomerId, BotId, MsgText, MsgType, Priority,
            FilePath, FileType, CampaignID, CampDescription,
            ScheduledSendDateTime, CreationDate,
            isSystemApproved, isAdminApproved, IsProcessed
        )
        SELECT
            CustomerId, BotId, MsgText, MsgType, Priority,
            FilePath, FileType, CampaignID, CampDescription,
            ScheduledSendDateTime, CreationDate,
            isSystemApproved, isAdminApproved, 1
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

/*******************************************
 * 2.11) usp_GetBotById
 *******************************************/

CREATE OR ALTER PROCEDURE dbo.usp_GetBotById
  @BotId           INT,
  @CustomerId			 INT
AS
BEGIN
  SET NOCOUNT ON;

  SELECT TOP 1 * FROM dbo.Bots
  WHERE BotId = @BotId
  AND CustomerId = @CustomerId
END
GO

/*******************************************
 * 2.12) usp_Bot_UpdateActivity
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_Bot_UpdateActivity
    @BotId    INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Bots
    SET IsActive = @IsActive
    WHERE BotId = @BotId;
END
GO

/*******************************************
 * 2.13) usp_Bot_CreateBot
 *******************************************/
CREATE OR ALTER PROCEDURE dbo.usp_Bot_CreateBot
    @CustomerId       INT,
    @IsActive         BIT,
    @PublicId         NVARCHAR(128),
    @EncryptedBotKey  NVARCHAR(128),
    @WebhookSecret    NVARCHAR(128),
    @WebhookUrl       NVARCHAR(512)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Bots (
        CustomerId,
        IsActive,
        PublicId,
        EncryptedBotKey,
        WebhookSecret,
        WebhookUrl,
        CreationDateTime
    )
    VALUES (
        @CustomerId,
        @IsActive,
        @PublicId,
        @EncryptedBotKey,
        @WebhookSecret,
        @WebhookUrl,
        GETDATE()
    );

    SELECT *
    FROM dbo.Bots
    WHERE BotId = SCOPE_IDENTITY();
END