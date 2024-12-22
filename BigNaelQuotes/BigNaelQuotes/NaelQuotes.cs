using System.Collections.Generic;

namespace BigNaelQuotes;

public struct NaelQuotes
{
    public List<Quote> Quotes { get; set; }
}

public struct Quote
{
    public int ID { get; set; }
    public Text Text { get; set; }
}

public struct Text
{
    public string DE { get; set; }
    public string EN { get; set; }
    public string FR { get; set; }
    public string JP { get; set; }
    public string CN { get; set; }
}