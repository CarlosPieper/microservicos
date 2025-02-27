﻿using CloudWeather.DataLoader.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appSettings.json")
    .AddEnvironmentVariables()
    .Build();

var servicesConfig = configuration.GetSection("Services");

var tempServiceConfig = servicesConfig.GetSection("Temperature");
var tempServiceHost = tempServiceConfig["Host"];
var tempServicePort = tempServiceConfig["Port"];

var precipServiceConfig = servicesConfig.GetSection("Precipitation");
var precipServiceHost = precipServiceConfig["Host"];
var precipServicePort = precipServiceConfig["Port"];

var zipCodes = new List<String>
{
    "12345",
    "91782",
    "47685",
    "61782",
    "19825",
    "82712",
};

Console.WriteLine("Starting data load");

var temperatureHttpClient = new HttpClient();
temperatureHttpClient.BaseAddress = new Uri($"http://{tempServiceHost}:{tempServicePort}");

var precipitationHttpClient = new HttpClient();
precipitationHttpClient.BaseAddress = new Uri($"http://{precipServiceHost}:{precipServicePort}");

foreach (var zip in zipCodes)
{
    Console.WriteLine($"Processing zip code: {zip}");
    var from = DateTime.Now.AddYears(-2);
    var thru = DateTime.Now;

    for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
    {
        var temps = PostTemp(zip, day, temperatureHttpClient);
        PostPrecip(temps[0], zip, day, precipitationHttpClient);
    }
}

void PostPrecip(int lowTemp, string zip, DateTime day, HttpClient precipitationHttpClient)
{
    var rand = new Random();
    var isPrecip = rand.Next(2) < 1;

    PrecipitationModel precipitation;

    if (isPrecip)
    {
        var precipInches = rand.Next(1, 16);
        if (lowTemp < 32)
        {
            precipitation = new PrecipitationModel
            {
                AmountInches = precipInches,
                WeatherType = "snow",
                ZipCode = zip,
                CreatedOn = day
            };
        }
        else
        {
            precipitation = new PrecipitationModel
            {
                AmountInches = precipInches,
                WeatherType = "rain",
                ZipCode = zip,
                CreatedOn = day
            };
        }
    }
    else
    {
        precipitation = new PrecipitationModel
        {
            AmountInches = 0,
            WeatherType = "none",
            ZipCode = zip,
            CreatedOn = day
        };
    }

    var precipResponse = precipitationHttpClient.PostAsJsonAsync("observation", precipitation).Result;
    if (precipResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Posted precipitation: Date {day} Zip: {zip} Type: {precipitation.WeatherType} Amount (in.): {precipitation.AmountInches}");
    }
    else
    {
        Console.WriteLine(precipResponse.ToString());
    }
}

List<int> PostTemp(string zip, DateTime day, HttpClient temperatureHttpClient)
{
    var rand = new Random();
    var t1 = rand.Next(0, 100);
    var t2 = rand.Next(0, 100);
    var hiLoTemps = new List<int> { t1, t2 };
    hiLoTemps.Sort();
    var temperatureObservation = new TemperatureModel
    {
        TempLowF = hiLoTemps[0],
        TempHighF = hiLoTemps[1],
        ZipCode = zip,
        CreatedOn = day
    };

    var tempResponse = temperatureHttpClient.PostAsJsonAsync("observation", temperatureObservation).Result;
    if (tempResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Posted temperature: Date {day} Zip: {zip} Lo (F): {hiLoTemps[0]} Hi (F): {hiLoTemps[1]}");
    }
    else
    {
        Console.WriteLine(tempResponse.ToString());
    }
    return hiLoTemps;
}