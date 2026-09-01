using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:9000/swagger/system/swagger.json");
        if (response.IsSuccessStatusCode) {
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Length: " + json.Length);
            Console.WriteLine(json.Substring(0, Math.Min(json.Length, 500)));
        } else {
            Console.WriteLine("Error: " + response.StatusCode);
        }
    }
}
