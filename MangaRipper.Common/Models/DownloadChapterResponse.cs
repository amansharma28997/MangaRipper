﻿using System;

namespace MangaRipper.Core.Models
{
    public class DownloadChapterResponse
    {
        public bool Error { get; internal set; }
        public Exception Exception { get; internal set; }
    }
}
