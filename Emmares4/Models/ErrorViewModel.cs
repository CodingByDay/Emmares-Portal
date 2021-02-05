using System;

namespace Emmares4.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}