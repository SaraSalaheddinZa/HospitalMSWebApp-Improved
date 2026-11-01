/* Bind new recovery links to the account identity/credential version at issuance.
   Historical tokens remain stored without guessing their original account version.
   Such unversioned links cannot be redeemed; request a fresh recovery link instead. */
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF OBJECT_ID(N'gh.PasswordResetTokens',N'U') IS NULL OR COL_LENGTH(N'gh.UserAccounts',N'AuthVersion') IS NULL
    THROW 51040, 'Apply the core and authentication migrations first.', 1;
BEGIN TRANSACTION;
IF COL_LENGTH(N'gh.PasswordResetTokens',N'IssuedAuthVersion') IS NULL
    ALTER TABLE gh.PasswordResetTokens ADD IssuedAuthVersion int NULL;
COMMIT TRANSACTION;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'gh.PasswordResetTokens') AND name=N'IX_PasswordResetTokens_Hash')
    CREATE INDEX IX_PasswordResetTokens_Hash ON gh.PasswordResetTokens(TokenHash) INCLUDE(UserId,IssuedAuthVersion,ExpiresAt,UsedAt);
GO
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'gh.PasswordResetTokens') AND name=N'CK_PasswordResetTokens_AuthVersion')
    ALTER TABLE gh.PasswordResetTokens WITH CHECK ADD CONSTRAINT CK_PasswordResetTokens_AuthVersion CHECK(IssuedAuthVersion IS NULL OR IssuedAuthVersion>0);
GO
