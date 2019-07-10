﻿using MangaRipper.Core.Logging;
using MangaRipper.Core.Models;
using MangaRipper.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaRipper.Plugin.MangaReader
{
    /// <summary>
    /// Support find chapters and images from MangaReader
    /// </summary>
    public class MangaReader : IPlugin
    {
        private static ILogger logger;
        private readonly IHttpDownloader downloader;
        private readonly IXPathSelector selector;

        public MangaReader(ILogger myLogger, IHttpDownloader downloader, IXPathSelector selector)
        {
            logger = myLogger;
            this.downloader = downloader;
            this.selector = selector;
        }
        public async Task<IEnumerable<Chapter>> GetChapters(string manga, IProgress<int> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            // find all chapters in a manga
            string input = await downloader.GetStringAsync(manga, cancellationToken);
            var title = selector.Select(input, "//h2[@class='aname']").InnerText;
            var chaps = selector
                .SelectMany(input, "//table[@id='listing']//a")
                .Select(n =>
                {
                    string url = n.Attributes["href"];
                    var resultUrl = new Uri(new Uri(manga), url).AbsoluteUri;
                    return new Chapter(n.InnerText, resultUrl);
                });
            // reverse chapters order and remove duplicated chapters in latest section
            chaps = chaps.Reverse().GroupBy(x => x.Url).Select(g => g.First()).ToList();
            // transform pages link
            progress.Report(100);
            return chaps;
        }

        public async Task<IEnumerable<string>> GetImages(string chapterUrl, IProgress<int> progress, CancellationToken cancellationToken)
        {
            // find all pages in a chapter
            string input = await downloader.GetStringAsync(chapterUrl, cancellationToken);
            var pages = selector.SelectMany(input, "//select[@id='pageMenu']/option")
                .Select(n => n.Attributes["value"]);

            // transform pages link
            pages = pages.Select(p =>
            {
                var value = new Uri(new Uri(chapterUrl), p).AbsoluteUri;
                return value;
            }).ToList();

            // find all images in pages
            int current = 0;
            var images = new List<string>();
            foreach (var page in pages)
            {
                var pageHtml = await downloader.GetStringAsync(page, cancellationToken);
                var image = selector
                .Select(pageHtml, "//img[@id='img']").Attributes["src"];
                images.Add(image);
                var f = (float)++current / pages.Count();
                int i = Convert.ToInt32(f * 100);
                progress.Report(i);
            }

            return images;
        }


        public SiteInformation GetInformation()
        {
            return new SiteInformation(nameof(MangaReader), "https://www.mangareader.net", "English");
        }

        public bool Of(string link)
        {
            var uri = new Uri(link);
            return uri.Host.Equals("www.mangareader.net");
        }
    }
}
