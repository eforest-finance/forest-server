using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace DefaultNamespace;

public class ProgramData
{
    static readonly HttpClient client = new HttpClient();



    [Fact]
    public async Task ForDiffData()
    {
        string notable = "/Users/weihubin/data/notable.txt";
        HashSet<string> symbolsInNotable = new HashSet<string>(File.ReadAllLines(notable));
        
        string explorer = "/Users/weihubin/data/explorer.txt";
        HashSet<string> symbolsInExplorer= new HashSet<string>(File.ReadAllLines(explorer));
        
        string path = "/Users/weihubin/data/diff_notable_explorer.txt"; 
        symbolsInNotable.ExceptWith(symbolsInExplorer);
        File.WriteAllLines(path, symbolsInNotable);
    }

    [Fact]
    public async Task ForData()
    {
        HashSet<string> allSymbols = await FetchAllSymbols("https://explorer-test.aelf.io/api/viewer/getAllTokens", 50);

        string path = "/Users/weihubin/data/explorer.txt";
        
        string pathA = "/Users/weihubin/data/uniq.txt";
        HashSet<string> symbolsInA = new HashSet<string>(File.ReadAllLines(pathA));
        symbolsInA.ExceptWith(allSymbols);
        File.WriteAllLines(pathA, symbolsInA);

        string pathB = "/Users/weihubin/data/notable.txt";
        HashSet<string> symbolsInB = new HashSet<string>(File.ReadAllLines(pathB));
        foreach (var symbol in allSymbols)
        {
            
            File.AppendAllText(path, symbol + Environment.NewLine);

            if (!symbolsInB.Contains(symbol))
            {
                File.AppendAllText(pathB, symbol + Environment.NewLine);
            }
        }
    }

    static async Task<HashSet<string>> FetchAllSymbols(string baseUrl, int pageSize)
    {
        HashSet<string> symbols = new HashSet<string>();
        int pageNum = 1;
        bool hasMoreData;

        do
        {
            string url = $"{baseUrl}?pageSize={pageSize}&pageNum={pageNum}";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseBody);
            JArray tokens = (JArray)jsonResponse["data"]["list"];

            if (tokens.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                foreach (var token in tokens)
                {
                    string symbolTemp = token["symbol"].ToString().Split("-")[0];
                    if (!symbolTemp.Any(char.IsDigit))
                    {
                        symbols.Add(symbolTemp.ToLower());
                    }
                }
                hasMoreData = tokens.Count == pageSize;
                pageNum++;
            }
        }
        while (hasMoreData);

        return symbols;
    }
}

