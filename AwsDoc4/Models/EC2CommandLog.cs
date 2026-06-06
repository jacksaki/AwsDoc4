using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Models;

public class EC2CommandLog
{
    public EC2CommandLog(string text, DateTime date)
    {
        Text = text;
        Date = date;
    }

    public string Text { get; }
    public DateTime Date { get; }
}
