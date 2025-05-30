using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public AttachmentRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<Attachment?> GetAttachment(Guid attachmentId)
    {
        try
        {
            const string query = @"
                SELECT
                    AttachmentId,
                    FileName,
                    BlobId,
                    DirectoryPath,
                    FileMIMEtype,
                    FileSize,
                    CreatedDate,
                    ModifiedDate,
                    CreatedByUpn,
                    ModifiedByUpn
                FROM [recognitionCitizen].[Attachment]
                WHERE AttachmentId = @AttachmentId;";

            return await _connection.QuerySingleOrDefaultAsync<Attachment>(query, new
            {
                AttachmentId = attachmentId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving attachment with AttachmentId {AttachmentId}", attachmentId);
            return null;
        }
    }

    public async Task<Attachment?> CreateAttachment(Attachment attachment)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[Attachment] (
                    FileName,
                    BlobId,
                    FileMIMEtype,
                    FileSize,
                    CreatedByUpn,
                    ModifiedByUpn
                )
                OUTPUT INSERTED.*
                VALUES (
                    @FileName,
                    @BlobId,
                    @FileMIMEtype,
                    @FileSize,
                    @CreatedByUpn,
                    @ModifiedByUpn
                );";

            return await _connection.QuerySingleAsync<Attachment>(query, new
            {
                attachment.FileName,
                attachment.BlobId,
                attachment.DirectoryPath,
                attachment.FileMIMEtype,
                attachment.FileSize,
                CreatedByUpn = "USER",      // TODO: replace once auth gets added
                ModifiedByUpn = "USER"      // TODO: replace once auth gets added
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error inserting attachment with FileName '{FileName}'", attachment.FileName);
            return null!;
        }
    }

    public async Task<bool> DeleteAttachment(Guid attachmentId)
    {
        try
        {
            const string query = @"
                DELETE FROM [recognitionCitizen].[Attachment]
                WHERE AttachmentId = @AttachmentId;";
            var rows = await _connection.ExecuteAsync(query, new
            {
                AttachmentId = attachmentId
            }, _transaction);
            
            return rows > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting attachment with AttachmentId {AttachmentId}", attachmentId);
            return false;
        }
    }

    public async Task<AttachmentLink?> GetAttachmentLink(Guid attachmentId, Guid linkId, Guid applicationId)
    {
        try
        {
            const string query = @"
                SELECT
                    AttachmentLinkId,
                    AttachmentId,
                    LinkId,
                    LinkTypeId,
                    ApplicationId,
                    CreatedDate,
                    ModifiedDate,
                    CreatedByUpn,
                    ModifiedByUpn
                FROM [recognitionCitizen].[AttachmentLink]
                WHERE AttachmentId = @AttachmentId
                  AND LinkId = @LinkId
                  AND ApplicationId = @ApplicationId;";

            return await _connection.QuerySingleOrDefaultAsync<AttachmentLink>(query, new
            {
                AttachmentId = attachmentId,
                LinkId = linkId,
                ApplicationId = applicationId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving attachment link with AttachmentId {AttachmentId}, LinkId {LinkId}, ApplicationId {ApplicationId}",
                attachmentId, linkId, applicationId);
            return null;
        }
    }

    public async Task<bool> CreateAttachmentLink(Guid attachmentId, Guid linkId, LinkTypeEnum linkTypeId, Guid applicationId)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[AttachmentLink] (
                    AttachmentId,
                    LinkId,
                    LinkTypeId,
                    ApplicationId,
                    CreatedByUpn,
                    ModifiedByUpn
                )
                VALUES (
                    @AttachmentId,
                    @LinkId,
                    @LinkTypeId,
                    @ApplicationId,
                    @CreatedByUpn,
                    @ModifiedByUpn
                );";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                AttachmentId = attachmentId,
                LinkId = linkId,
                LinkTypeId = (int)linkTypeId,
                ApplicationId = applicationId,
                CreatedByUpn = "USER",      // TODO: replace once auth gets added
                ModifiedByUpn = "USER"      // TODO: replace once auth gets added
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating attachment link for AttachmentId {AttachmentId} and LinkId {LinkId}", attachmentId, linkId);
            return false;
        }
    }
    public async Task<bool> DeleteAttachmentLink(Guid attachmentLinkId)
    {
        try
        {
            const string query = @"
                DELETE FROM [recognitionCitizen].[AttachmentLink]
                WHERE AttachmentLinkId = @AttachmentLinkId;";

            var rows = await _connection.ExecuteAsync(query, new
            {
                AttachmentLinkId = attachmentLinkId
            }, _transaction);

            return rows > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting attachment link with AttachmentLinkId {AttachmentLinkId}", attachmentLinkId);
            return false;
        }
    }
}