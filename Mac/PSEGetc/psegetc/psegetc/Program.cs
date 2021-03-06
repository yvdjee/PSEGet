﻿using System;
using System.IO;
using PSEGetLib;
using PSEGetLib.Interfaces;
using PSEGetLib.Service;
using PSEGetLib.DocumentModel;
using PSEGetLib.Converters;
using System.Reflection;

namespace psegetc
{
	class Program
	{
		static string _targetPath;
		static string _outputPath;
		static string _outputFormat;
		static string _dateFormat;
		static string _dateFrom;
		static string _dateTo;
		static string _reportsDir;

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				DisplayHelp();
				return;
			}

			try
			{
				Initialize();
				WorkIt();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		static void WorkIt()
		{			
			if (_targetPath == null)
			{
				throw new Exception("Error: Unspecified PSE report file path.");
			}

			if (_targetPath.Contains("http:"))
			{
				var downloadParams = new DownloadParams();
				downloadParams.BaseURL = _targetPath;
				downloadParams.FileName = "stockQuotes_%mm%dd%yyyy";
				downloadParams.FromDate = Convert.ToDateTime(_dateFrom);
				downloadParams.ToDate = Convert.ToDateTime(_dateTo);

				var downloader = new ReportDownloader(downloadParams, _reportsDir, null);
				downloader.AsyncMode = false;
				downloader.Download();

				foreach (DownloadFileInfo reportFile in downloader.DownloadedFiles)
				{
					if (reportFile.Success)
						ConvertIt(reportFile.Filename);
				}

			}
			else
			{				
				ConvertIt(_targetPath);
			}

		}

		static void ConvertIt(string fileToConvert)
		{
			IPdfService pdfService = new PdfTextSharpService();
			var pseDocument = new PSEDocument();
			IPSEReportReader reader = new PSEReportReader2();
			reader.Fill(pseDocument, pdfService.ExtractTextFromPdf(fileToConvert));
			if (_outputFormat.Contains("csv"))
			{
				string[] csvParam = _outputFormat.Split(':');
				string csvFormat = string.Empty;
				if (csvParam.Length == 2)
				{
					csvFormat = csvParam[1];
					csvFormat = csvFormat.Replace("S", "{S}");
					csvFormat = csvFormat.Replace("D", "{D}");
					csvFormat = csvFormat.Replace("O", "{O}");
					csvFormat = csvFormat.Replace("H", "{H}");
					csvFormat = csvFormat.Replace("L", "{L}");
					csvFormat = csvFormat.Replace("C", "{C}");
					csvFormat = csvFormat.Replace("V", "{V}");
					csvFormat = csvFormat.Replace("F", "{F}");
				}
				else
					csvFormat = "{S},{D},{O},{H},{L},{C},{V},{F}";

				var csvOutputSettings = new CSVOutputSettings();
				csvOutputSettings.CSVFormat = csvFormat;
				csvOutputSettings.DateFormat = _dateFormat;
				csvOutputSettings.Delimiter = ",";
				csvOutputSettings.Filename = Path.GetFileName(fileToConvert).Replace("pdf", "csv");
				csvOutputSettings.OutputDirectory = _outputPath;
				csvOutputSettings.UseSectorValueAsVolume = true;
				csvOutputSettings.SectorVolumeDivider = 1000;

				pseDocument.ToCSV(csvOutputSettings);
			}
		}

		static void Initialize()
		{
			string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			_reportsDir = Path.Combine(exeDir, "Reports");
			if (!Directory.Exists(_reportsDir))
				Directory.CreateDirectory(_reportsDir);

			_targetPath = GetParamValue("-t");

			_dateFrom = GetParamValue("-df");
			if (_dateFrom == "today" || _dateFrom == null)
			{
				_dateFrom = DateTime.Today.ToString("MM/dd/yyyy");
			}

			_dateTo = GetParamValue("-dt");
			if (_dateTo == null)
			{
				_dateTo = _dateFrom;
			}

			_outputPath = GetParamValue("-o");
			if (_outputPath == null)
			{
				_outputPath = Environment.CurrentDirectory;
			}

			_outputFormat = GetParamValue("-f");
			if (_outputFormat == null)
			{
				_outputFormat = "csv:S,D,O,H,L,C,V,F";
			}

			_dateFormat = GetParamValue("-d");
			if (_dateFormat == null)
			{
				_dateFormat = "MM/DD/YYYY";
			}
		}

		static string GetParamValue(string param)
		{
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == param)
				{
					if (i + 1 < args.Length)
						return args[i + 1];
				}
			}

			return null;
		}

		static void DisplayHelp()
		{
			Console.WriteLine("PSEGet Console. (c) 2016 Arnold Diaz.");
			Console.WriteLine("\tUsage: pseget -t [url | local path] -df [from date] -dt [to date] -o [output path] -f [csv:<format>] -d [date format]");
			Console.WriteLine("\t-t [url:<from: date to: date> | local path]");
			Console.WriteLine("\t\tPSE Report File Path.\n");
			Console.WriteLine("\t-df [from date]");
			Console.WriteLine("\t\tStart download from specified date.");
			Console.WriteLine("\t-dt [to date]");
			Console.WriteLine("\t\tOptional. Download to specified date. Date will default to -df date if not specified.\n");
			Console.WriteLine("\t-o [output path]");
			Console.WriteLine("\t\tOptional. Output Path. Defaults to executable path.\n");
			Console.WriteLine("\t-f [csv:<format>]");
			Console.WriteLine("\t\tOptional. Target Output. Defaults to CSV.");
			Console.WriteLine("\t\tCSV Optional <format> defaults to S,D,O,H,L,C,V,F\n");
			Console.WriteLine("\t-d [date format]");
			Console.WriteLine("\t\tOptional Date Format. Defaults to MM/dd/yyyy.\n");
			Console.WriteLine("Example 1 (Download range) : pseget -t http://www.pse.com.ph/resource/dailyquotationreport/file/ -df 06/20/2016 -dt 06/24/2016 ");
			Console.WriteLine("Example 2 (Download today) : pseget -t http://www.pse.com.ph/resource/dailyquotationreport/file/ -df today");
			Console.WriteLine("Example 3 (From file)      : pseget -t /User/Reports/stockQuotes_552016.pdf");
			Console.WriteLine("Example 4 (Typical)        : pseget -t http://www.pse.com.ph/resource/dailyquotationreport/file/ -df 06/20/2016 -dt 06/24/2016 -o /User/myfolder/");
			Console.WriteLine("Example 5 (CSV)            : pseget -t /User/Reports/stockQuotes_552016.pdf -o /User/Reports/myfolder/ -f csv:S,D,C");
		}
	}
}
