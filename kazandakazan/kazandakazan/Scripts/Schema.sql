/*
  Kazandakazan — referans SQL şeması (SQL Server).
  Projede şema genelde EF Core migrations ile üretilir; bu dosya gözden geçirme ve DBA paylaşımı içindir.
*/

CREATE TABLE [Users] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(128) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(512) NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL
);
CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE TABLE [Pots] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [DisplayName] NVARCHAR(256) NULL,
    [CurrentBalance] DECIMAL(18,2) NOT NULL,
    [TargetAmount] DECIMAL(18,2) NOT NULL,
    [Status] TINYINT NOT NULL, -- 0 Açık, 1 Kapalı
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [ClosedAtUtc] DATETIME2 NULL,
    [WinnerUserId] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [FK_Pots_Users_WinnerUserId] FOREIGN KEY ([WinnerUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [PotTransactions] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [PotId] INT NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Status] TINYINT NOT NULL, -- 0 Bekliyor, 1 Tamam, 2 Başarısız
    [ExternalPaymentId] NVARCHAR(128) NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [CompletedAtUtc] DATETIME2 NULL,
    CONSTRAINT [FK_PotTransactions_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_PotTransactions_Pots] FOREIGN KEY ([PotId]) REFERENCES [Pots] ([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_PotTransactions_PotId] ON [PotTransactions] ([PotId]);
CREATE INDEX [IX_PotTransactions_UserId] ON [PotTransactions] ([UserId]);
CREATE UNIQUE INDEX [IX_PotTransactions_PotId_ExternalPaymentId]
    ON [PotTransactions] ([PotId], [ExternalPaymentId])
    WHERE [ExternalPaymentId] IS NOT NULL;

CREATE TABLE [Tickets] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [PotId] INT NOT NULL,
    [PotTransactionId] BIGINT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [FK_Tickets_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Tickets_Pots] FOREIGN KEY ([PotId]) REFERENCES [Pots] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Tickets_PotTransactions] FOREIGN KEY ([PotTransactionId]) REFERENCES [PotTransactions] ([Id])
);
CREATE INDEX [IX_Tickets_PotId] ON [Tickets] ([PotId]);
CREATE INDEX [IX_Tickets_UserId_PotId] ON [Tickets] ([UserId], [PotId]);
