using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utils
{
	public sealed class FileStorage
	{
		private readonly string _folderPath;

		public FileStorage(string folderPath)
		{
			if (string.IsNullOrWhiteSpace(folderPath))
			{
				throw new ArgumentNullException(nameof(folderPath));
			}

			_folderPath = folderPath.Replace('\\', '/').Trim().ToLower();

			if (!Path.IsPathFullyQualified(_folderPath))
			{
				throw new ArgumentException($"{nameof(folderPath)} must be fully qualified", nameof(folderPath));
			}

			Directory.CreateDirectory(_folderPath);
		}

		private string FormatFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			fileName = fileName.Replace('\\', '/').Trim().Replace(":", "-c-o-l-o-n-").ToLower();

			if (Path.IsPathFullyQualified(fileName))
			{
				throw new ArgumentException($"{nameof(fileName)} must be relative", nameof(fileName));
			}

			return Path.Combine(_folderPath, fileName).Replace('\\', '/').ToLower();
		}

		public async Task WriteFileAsync(string fileName, string fileContent, CancellationToken cancellationToken)
		{
			fileName = FormatFileName(fileName);
			
			if (string.IsNullOrWhiteSpace(fileContent))
			{
				throw new ArgumentNullException(nameof(fileContent));
			}

			var fileFolder = Path.GetDirectoryName(fileName).ToLower().Replace('\\', '/');

			Directory.CreateDirectory(fileFolder);

			await File.WriteAllTextAsync(fileName, fileContent, cancellationToken);
		}

		public async Task<string> ReadFileAsync(string fileName, CancellationToken cancellationToken)
		{
			fileName = FormatFileName(fileName);

			return await File.ReadAllTextAsync(fileName, cancellationToken);
		}

		public bool ContainsFile(string fileName)
		{
			fileName = FormatFileName(fileName);

			return File.Exists(fileName);
		}

		public void RemoveFile(string fileName)
		{
			fileName = FormatFileName(fileName);

			try
			{
				File.Delete(fileName);
			}
			catch (DirectoryNotFoundException)
			{
				// ignore
			}
			catch (FileNotFoundException)
			{
				// ignore
			}
		}

		public void ClearFiles()
		{
			try
			{
				Directory.Delete(_folderPath, true);
			}
			catch (DirectoryNotFoundException)
			{
				// ignore
			}

			Directory.CreateDirectory(_folderPath);
		}

		public IEnumerable<(string FilePath, Action RemoveFile)> RemoveFilesExcept(IEnumerable<string> fileNamesToKeep)
		{
			if (fileNamesToKeep is null)
			{
				throw new ArgumentNullException(nameof(fileNamesToKeep));
			}

			var filePathsToKeep = fileNamesToKeep.Where(fileName => !string.IsNullOrWhiteSpace(fileName)).Select(FormatFileName).ToList();
			var filePathsToDelete = Directory.GetFiles(_folderPath, "*", SearchOption.AllDirectories).Select(x => x.Replace('\\', '/')).Except(filePathsToKeep).ToList();

			return filePathsToDelete.Select(filePath => (FilePath: filePath, RemoveFile: (Action)(() =>
			{
				try
				{
					File.Delete(filePath);
				}
				catch (DirectoryNotFoundException)
				{
					// ignore
				}
				catch (FileNotFoundException)
				{
					// ignore
				}
			}))).ToList();
		}
	}
}