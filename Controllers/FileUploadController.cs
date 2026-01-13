using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NodeCasperParser.Models;

using Microsoft.AspNetCore;

using Npgsql;
using Casper.Network.SDK.Types;
using NodeCasperParser.Helpers;
using System.Runtime;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections;

using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Diagnostics;
using SixLabors.ImageSharp.Compression;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FileUpload
{
    public class FileUploadFilter : IOperationFilter // For Swagger file upload
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formParameters = context.ApiDescription.ParameterDescriptions
                .Where(paramDesc => paramDesc.IsFromForm());
            // already taken care by swashbuckle
            if (formParameters.Any())
            {
                return;
            }
            if (operation.RequestBody != null)
            {
                return;
            }
            if (context.ApiDescription.HttpMethod == HttpMethod.Post.Method)
            {
                var uploadFileMediaType = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema()
                    {
                        Type = "object",
                        Properties =
                    {
                        ["files"] = new OpenApiSchema()
                        {
                            Type = "array",
                            Items = new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        }
                    },
                        Required = new HashSet<string>() { "files" }
                    }
                };

                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = { ["multipart/form-data"] = uploadFileMediaType }
                };
            }
        }
    }

    public static class Helper // for swagger file upload
    {
        internal static bool IsFromForm(this ApiParameterDescription apiParameter)
        {
            var source = apiParameter.Source;
            var elementType = apiParameter.ModelMetadata?.ElementType;

            return (source == BindingSource.Form || source == BindingSource.FormFile)
                || (elementType != null && typeof(IFormFile).IsAssignableFrom(elementType));
        }
    }

    public class FilesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        bool debugMode = false;

       // [ApiExplorerSettings(IgnoreApi = true)]
        public FilesController(IConfiguration configuration, IWebHostEnvironment env)
        {
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
            _configuration = configuration;
            _env = env;
        }

        public class ImageModel
        {
         //   [FromForm(Name = "avatar")]
         //   public IFormFile Avatar { get; set; }
                       
            public string UserId { get; set; }
            //  public byte[] Bytes { get; set; }
            public IFormCollection Image { get; set; }
        }          
    }
}