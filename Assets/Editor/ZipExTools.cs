using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Compression ;
using System.IO ;

public class ZipExTools  {

	public static void ExportZip(string readPath , string outputPath){
		
	}

	public static void Decompress(FileInfo fileToDecompress)
	{
//		using (FileStream originalFileStream = fileToDecompress.OpenRead())
//		{
//			string currentFileName = fileToDecompress.FullName;
//			string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);
//
//			using (FileStream decompressedFileStream = File.Create(newFileName))
//			{
//				using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
//				{
//					
////					decompressionStream.CopyTo(decompressedFileStream);
////					Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
//				}
//			}
//		}
//		using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
//		{
//			archive.CreateEntryFromFile(newFile, "NewEntry.txt");
//			archive.ExtractToDirectory(extractPath);
//		} 

	}
}
