using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks
{
    public class ApiResponse
    {
        public ApiResponse()
        {
            status = 200;
            message = "Success";

        }
        public ApiResponse(int status, string message)
        {
            this.status = status;
            this.message = message;
           
        }
        public int status { get; set; }
        public string? InsertedId { get; set; }
        public string? message { get; set; }
        public object? data { get; set; }
        public object? errors { get; set; }
    }
}

