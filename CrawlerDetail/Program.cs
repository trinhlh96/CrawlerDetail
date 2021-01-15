using HtmlAgilityPack;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerDetail
{
    class Program
    {
        static void Main(string[] args)
        {
            EndCawler();
            Console.ReadLine();
        }
        private static void EndCawler()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "amqHelloQueue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var link = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Received {0}", link);
                    var url = link;

                    var httpClient = new HttpClient();
                    var html = await httpClient.GetStringAsync(url);
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(html);



                    var divs = htmlDocument.DocumentNode.Descendants("section").Where(node => node.GetAttributeValue("class", "").Equals("section page-detail top-detail")).FirstOrDefault();
                    var title = divs.Descendants("div").FirstOrDefault().Descendants("h1").FirstOrDefault().InnerText;
                    var dess = divs.Descendants("div").FirstOrDefault().Descendants("article").FirstOrDefault().Descendants("p").ToList();
                    var description = "";
                    foreach (var des in dess)
                    {

                        description += des.InnerText;

                    }
                    var image = "";
                    if (divs.Descendants("div")
                        .FirstOrDefault()
                        .Descendants("article")
                        .FirstOrDefault()
                        .Descendants("img")
                        .FirstOrDefault() != null && divs.Descendants("div")
                                        .FirstOrDefault()
                                        .Descendants("article")
                                        .FirstOrDefault()
                                        .Descendants("img")
                                        .FirstOrDefault()
                                        .ChildAttributes("data-src")
                                        .FirstOrDefault() != null)
                    {
                        image = divs.Descendants("div")
                                        .FirstOrDefault()
                                        .Descendants("article")
                                        .FirstOrDefault()
                                        .Descendants("img")
                                        .FirstOrDefault()
                                        .ChildAttributes("data-src")
                                        .FirstOrDefault().Value;

                    }

                    Console.WriteLine("Save Data thành công");
                    var crawler = new Crawler
                    {
                        Title = title,
                        Image = image,
                        Description = description
                    };


                    DbContextCrawler dbContextCrawler = new DbContextCrawler();
                    if (!dbContextCrawler.Crawlers.Any(o => o.Title == crawler.Title))
                    {
                        dbContextCrawler.Crawlers.Add(crawler);
                        dbContextCrawler.SaveChanges();
                    }

                };
                channel.BasicConsume(queue: "amqHelloQueue",
                                             autoAck: true,
                                             consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
