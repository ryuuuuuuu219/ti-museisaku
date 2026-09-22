using UnityEngine;

public static class Analysis
{
    public static string reply(int phase, string text)
    {
        string replyText = "";
        if (text.Contains("ぬるぽ"))
        {
            replyText = "user:ぬるぽ\nreply:ガッ";
        }
        else
        {
            replyText = "?";
        }
        return replyText;
    }
}
