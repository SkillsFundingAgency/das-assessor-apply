using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Web.Infrastructure;

namespace SFA.DAS.ApplyService.Web.Controllers
{
    public class UploadController : Controller
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<UploadController> _logger;
        private readonly IApplicationApiClient _client;

        public UploadController(IStorageService storageService, ILogger<UploadController> logger, IApplicationApiClient client)
        {
            _storageService = storageService;
            _logger = logger;
            _client = client;
        }


        [HttpPost]
        public async Task<IActionResult> Chunks()
        {
            var formItems = await HttpContext.Request.ReadFormAsync();

            var chunkParameters = GetChunkParameters(formItems);

            await SaveFile(chunkParameters);

            await TryAssembleFile(chunkParameters);

            return Ok();
        }

        private static ChunkParameters GetChunkParameters(IFormCollection formItems)
        {
            return new ChunkParameters()
            {
                Chunks = int.Parse(formItems["resumableTotalChunks"][0]),
                ChunkNumber = int.Parse(formItems["resumableChunkNumber"][0]),
                Identifier = formItems["resumableIdentifier"][0],
                ApplicationId = formItems["applicationId"][0],
                SequenceId = int.Parse(formItems["sequenceId"][0]),
                SectionId = int.Parse(formItems["sectionId"][0]),
                PageId = formItems["page"][0],
                QuestionId = formItems["questionId"][0],
                FileName = formItems["resumableFilename"][0],
                Type = formItems["resumableType"][0],
                File = formItems.Files[0]
            };
        }

        private async Task SaveFile(ChunkParameters chunkParameters)
        {
            var fileStream = new MemoryStream();
            var openReadStream = chunkParameters.File.OpenReadStream();
            openReadStream.CopyTo(fileStream);

            fileStream.Position = 0;

            await _storageService.Store(chunkParameters.ApplicationId, chunkParameters.SequenceId,
                chunkParameters.SectionId, chunkParameters.PageId, chunkParameters.QuestionId,
                $"{chunkParameters.Identifier}_{chunkParameters.ChunkNumber}", fileStream, chunkParameters.File.ContentType);
        }

        private async Task TryAssembleFile(ChunkParameters chunkParameters)
        {
            if (await AllChunksAreHere(chunkParameters))
            {
                // Create a single file
                var assembledFileStream = await ConsolidateFile(chunkParameters);

                // Rename consolidated with original name of upload
                await _storageService.Store(chunkParameters.ApplicationId, chunkParameters.SequenceId,
                    chunkParameters.SectionId, chunkParameters.PageId, chunkParameters.QuestionId,
                    chunkParameters.FileName, assembledFileStream, chunkParameters.Type);

                // Delete chunk files
                await DeleteChunks(chunkParameters);

                var userId = User.GetUserId();
                
                // Save file into QnA
                await _client.UpdateFileUploadAnswer(chunkParameters.ApplicationId, chunkParameters.SequenceId, chunkParameters.SectionId, 
                    chunkParameters.PageId, chunkParameters.QuestionId, chunkParameters.FileName, userId);
            }
        }

        private async Task DeleteChunks(ChunkParameters chunkParameters)
        {
            for (var chunkNumber = 1; chunkNumber <= chunkParameters.Chunks; chunkNumber++)
            {
                var chunkFileName = $"{chunkParameters.Identifier}_{chunkNumber}";
                _logger.LogInformation($"Deleting: {chunkFileName}");
                if (await ChunkIsHere(chunkNumber, chunkParameters))
                {
                    await _storageService.Delete(Guid.Parse(chunkParameters.ApplicationId), chunkParameters.SequenceId, chunkParameters.SectionId, chunkParameters.PageId, chunkParameters.QuestionId, chunkFileName);
                }
            }
        }

        private async Task<Stream> ConsolidateFile(ChunkParameters chunkParameters)
        {
            // create destination memory stream
            var dest = new MemoryStream();

            for (var chunkNumber = 1; chunkNumber <= chunkParameters.Chunks; chunkNumber++)
            {
                var chunkFileName = $"{chunkParameters.Identifier}_{chunkNumber}";
                var chunk = await _storageService.Retrieve(chunkParameters.ApplicationId, chunkParameters.SequenceId, chunkParameters.SectionId, chunkParameters.PageId, chunkParameters.QuestionId, chunkFileName);

                var ms = new MemoryStream();

                chunk.Item2.CopyTo(ms);

                dest.Write(ms.ToArray(), 0, (int) chunk.Item2.Length);
            }

            dest.Position = 0;

            return dest;
        }

        private async Task<bool> AllChunksAreHere(ChunkParameters chunkParameters)
        {
            for (var chunkNumber = 1; chunkNumber <= chunkParameters.Chunks; chunkNumber++)
                if (!await ChunkIsHere(chunkNumber, chunkParameters))
                    return false;
            return true;
        }

        private async Task<bool> ChunkIsHere(int chunkNumber, ChunkParameters chunkParameters)
        {
            var fileName = $"{chunkParameters.Identifier}_{chunkNumber}";
            return await _storageService.Exists(chunkParameters.ApplicationId, chunkParameters.SequenceId,
                chunkParameters.SectionId, chunkParameters.PageId, chunkParameters.QuestionId,
                fileName);
        }
    }

    class ChunkParameters
    {
        public int Chunks { get; set; }
        public string Identifier { get; set; }
        public string ApplicationId { get; set; }
        public int SequenceId { get; set; }
        public int SectionId { get; set; }
        public string PageId { get; set; }
        public string QuestionId { get; set; }
        public string FileName { get; set; }
        public string Type { get; set; }
        public int ChunkNumber { get; set; }
        public IFormFile File { get; set; }
    }
}