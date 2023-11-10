
namespace CrewLink.WindowsServices.Files
{
    using Crewlink.WindowsServices.Features;
    using CrewlinkServices.Core.DataAccess;
    using CrewlinkServices.Core.Identity;
    using CrewlinkServices.Models.CustomModel;
    using CrewlinkServices.Models.DB;
    using CrewlinkServices.Models.ViewModel;
    using System;
    using System.Net.Http;
    using iTextSharp.text;
    using System.Net;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using System.Threading.Tasks;
    using CrewlinkServices.Core.Request.Response;
    using iTextSharp.text.pdf;
    using iTextSharp.tool.xml.html;
    using CrewlinkServices.Core.Results;
    using iTextSharp.tool.xml.css;
    using iTextSharp.tool.xml;
    using iTextSharp.tool.xml.pipeline.html;
    using System.Net.Http.Headers;
    using iTextSharp.tool.xml.pipeline.end;
    using iTextSharp.tool.xml.pipeline.css;
    using iTextSharp.tool.xml.parser;
    using System.Threading;

    //using Crewlink.WindowsServices.Features;
    //using CrewLink.WindowsServices.Features;

    public static class GenerateZipFolder
    {
        
        public static HttpResponseMessage ExecuteResult(string FileContent, string TemplateSize, int NumberOfPages, string FileName)
        {
            //  var _response = new FileResponseBase();
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            StringReader responseContent = new StringReader(FileContent);
            byte[] bytes;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                Document pdfDoc;
                if (TemplateSize.Equals("A3"))
                {
                    pdfDoc = new Document(PageSize.A3, 30f, 30f, 20f, 20f);
                    pdfDoc.SetPageSize(PageSize.A3.Rotate());
                }
                else
                {
                    pdfDoc = new Document(PageSize.A4, 30f, 30f, 20f, 20f);
                }

                using (var doc = pdfDoc)
                {
                    var writer = PdfWriter.GetInstance(doc, memoryStream);
                    doc.Open();
                    //  doc.Add(new Chunk(""));
                    var tagProcessors = (DefaultTagProcessorFactory)Tags.GetHtmlTagProcessorFactory();
                    tagProcessors.RemoveProcessor(HTML.Tag.IMG); // remove the default processor
                    tagProcessors.AddProcessor(HTML.Tag.IMG, new CustomImageTagProcessor()); // use custom processor

                    //var tagProcessorFactory = Tags.GetHtmlTagProcessorFactory();
                    tagProcessors.AddProcessor(
                        new TableDataProcessor(),
                        new string[] { HTML.Tag.TD }
                    );

                    var verticalAlignCss = @".verticalTableHeader {
                            text-align: center;
                            white-space: nowrap;
                            -webkit-transform: rotate(90deg);
                            writing-mode: vertical-lr;
                            }";

                    CssFilesImpl cssFiles = new CssFilesImpl();
                    cssFiles.Add(XMLWorkerHelper.GetInstance().GetDefaultCSS());
                    var cssResolver = new StyleAttrCSSResolver(cssFiles);
                    cssResolver.AddCss(@"code { padding: 2px 4px; }", "utf-8", true);
                    cssResolver.AddCss(verticalAlignCss, "utf-8", true);
                    var charset = Encoding.UTF8;
                    var hpc = new HtmlPipelineContext(new CssAppliersImpl(new XMLWorkerFontProvider()));
                    hpc.SetAcceptUnknown(true).AutoBookmark(true).SetTagFactory(tagProcessors); // inject the tagProcessors
                    var htmlPipeline = new HtmlPipeline(hpc, new PdfWriterPipeline(doc, writer));
                    var pipeline = new CssResolverPipeline(cssResolver, htmlPipeline);
                    var worker = new XMLWorker(pipeline, true);
                    var xmlParser = new XMLParser(true, worker, charset);
                    xmlParser.Parse(responseContent);
                }
                bytes = memoryStream.ToArray();
            }

            if (NumberOfPages.Equals(0))
            {
                Font blackFont = FontFactory.GetFont("Tahoma", 9, Font.NORMAL, BaseColor.BLACK);
                using (MemoryStream stream = new MemoryStream())
                {
                    PdfReader reader = new PdfReader(bytes);
                    using (PdfStamper stamper = new PdfStamper(reader, stream))
                    {
                        int pages = reader.NumberOfPages;
                        for (int i = 1; i <= pages; i++)
                        {
                            ColumnText.ShowTextAligned(stamper.GetUnderContent(i), Element.ALIGN_RIGHT, new Phrase(i.ToString(), blackFont), 560f, 15f, 0);
                        }
                    }
                    bytes = stream.ToArray();
                }
            }

            response.Content = new StreamContent(new MemoryStream(bytes));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.Add("Content-Disposition", "inline; filename=" + FileName + ".pdf");
            // ClearTempImage(_response.FileName);
            return response;
        }


    }


    public class ReportTemplateUrlViewModel
    {
        public string UserName { get; set; }
        public List<FileDetail> FileList { get; set; }
        public string dfrType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public class FileDetail
        {
            public string FileName { get; set; }
            public string Token { get; set; }
        }

    }
    public class FileResponseBase : ResponseBase
    {
        public string FileContent { get; set; }

        public MemoryStream StreamContent { get; set; }

        public string FileName { get; set; }

        public string TemplateSize { get; set; } = "A4";

        public int NumberOfPages { get; set; } = 0;

        public int CurrentPage { get; set; }
    }
}
