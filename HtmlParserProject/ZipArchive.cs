using System;
using System.ComponentModel;
using System.IO;
using Interface;
using Java.Util;
using ICSharpCode.SharpZipLib.Zip;


namespace AmazonPriceChecker_mono
{
	public class ZipArchive: IDecompress
	{
		public event DelUnzipStatus EvtUnzipStatus;
		public event DelUnzipProgressChanges EvtProgressChanges;
		public event DelUnzipFinished EvtUnzipFinished;

		private readonly String[] _zipFiles;
		private long _per;
		private readonly BackgroundWorker _worker;
		private long _size = 0;
		Timer timer;

		struct BackgroundWorkerResultType
		{
			public bool Success;
			public string Message;
		}

		public ZipArchive()
		{
//			LibraryFeature features = SevenZipExtractor.CurrentLibraryFeatures;
//			SevenZipExtractor.SetLibraryPath ("C:\\Users\\mike\\Dropbox\\APC-mono_out\\APC-mono_out\\7z.dll");
//			SevenZipExtractor extr = new SevenZipExtractor (path);
//
//			extr.Extracting += ChangeProgress;
//			extr.ExtractionFinished += HandleExtractionFinished;
//			extr.ExtractArchive (path);
	}
		public ZipArchive (string[] zipFiles)
		{

			_zipFiles = zipFiles;     

			_worker = new BackgroundWorker ();
			_worker.DoWork += WorkerUnZip;
			_worker.RunWorkerCompleted += WorkerFinishUnZip;
			_worker.WorkerReportsProgress = true;
			_worker.ProgressChanged += WorkerProgressChangedUnZip;
		}

//		void HandleExtractionFinished (object sender, EventArgs e)
//		{
//			EvtUnzipFinished(true, null);
//		}
//
//		void ChangeProgress (object sender, ProgressEventArgs e)
//		{
//			EvtProgressChanges(e.PercentDone, null);
//		}

		public void UnZip ()
		{
			_worker.RunWorkerAsync ();
		}

		private void WorkerProgressChangedUnZip (object sender, ProgressChangedEventArgs e)
		{
			if (e.ProgressPercentage == 0) {
				EvtUnzipStatus (e.UserState as string);
			} else {
				EvtProgressChanges (e.ProgressPercentage, e.UserState as string);
			}
		}

		private void WorkerFinishUnZip (object sender, RunWorkerCompletedEventArgs e)
		{
			BackgroundWorkerResultType result = (BackgroundWorkerResultType)e.Result;
			EvtUnzipFinished (result.Success, result.Message);
			if (timer != null)
				timer.Cancel ();
		}

		private void WorkerUnZip (object sender, DoWorkEventArgs arg)
		{
			BackgroundWorkerResultType result = new BackgroundWorkerResultType ();
			timer = new Timer ();
			// Schedule timer for report progress
			RenewProgress progress = new RenewProgress ();
			progress.decompress = this;
			timer.Schedule (progress, 0, 1000);
			try {

				// DotNetLibrary
//					foreach (string filename in _zipFiles) {
//						string zipToUnpack = filename;
//						if (ZipFile.IsZipFile (filename)) {
//							Java.IO.File file = new Java.IO.File (filename);
//							_size = file.Length ();
//							using (ZipFile zip1 = ZipFile.Read(zipToUnpack)) {
//								// here, we extract every entry, but we could extract conditionally
//								// based on entry name, size, date, checkbox status, etc.  
//								foreach (ZipEntry e in zip1) {
//									if (e.IsDirectory) {
//										//System.IO.File fmd = new System.IO.File(path + filename);
//										Directory.CreateDirectory (Path.Combine(StaticHolder.TmpDownloadPath, e.FileName));
//										continue;
//									}
//									_per += e.CompressedSize;
//
//							    _worker.ReportProgress(0, "Extract " + e.FileName);
//
//							    using (FileStream fout = new FileStream((Path.Combine(StaticHolder.TmpDownloadPath, e.FileName)), FileMode.Create))
//							    {
//							        try
//							        {
//									e.Extract (fout);
//							    }
//							        catch (Exception ex)
//							        {
//                                        //extract fail!
//                                        result.Success = false;
//                                        result.Message = ex.Message;
//
//                                        arg.Result = result;
//							            return;
//							        }
//
//							    }
//
//
//                                _worker.ReportProgress((int) (_per * 100 / _size), "Finish extracting " + e.FileName);
//
//
//								}
//							}
//						}
				//SharpZipLib
				foreach (string filename in _zipFiles) {
					_worker.ReportProgress (0, "Extract  file:" + filename);
					using (ZipInputStream s = new ZipInputStream(System.IO.File.OpenRead(filename))) {
					
						ZipEntry theEntry;
						while ((theEntry = s.GetNextEntry()) != null) {
							string directoryName = Path.GetDirectoryName (theEntry.Name);
							string fileName = Path.GetFileName (theEntry.Name);
							_per += theEntry.CompressedSize;
							// create directory
							if (directoryName.Length > 0) {
								Directory.CreateDirectory (Path.Combine(StaticHolder.TmpDownloadPath,directoryName));
							}
					
							if (fileName != String.Empty) {
								using (FileStream streamWriter = System.IO.File.Create(Path.Combine(StaticHolder.TmpDownloadPath, theEntry.Name))) {
									int size = 2048;
									byte[] data = new byte[2048];
									while (true) {
										size = s.Read (data, 0, data.Length);
										if (size > 0) {
											streamWriter.Write (data, 0, size);
										} else {
											break;
										}
									}
								}
							}
						}
									
					}
					_worker.ReportProgress (100, "Extract finished");
				}

				//delete downloaded zip files
				foreach (string filename in _zipFiles) {
					File.Delete (filename);
				}

			
				//delete old data
				Directory.Delete (StaticHolder.DownloadPath, true);

				//move the new data from tmp to data download path
				Directory.Move (StaticHolder.TmpDownloadPath, StaticHolder.DownloadPath);
				
				result.Success = true;
				result.Message = "Finish downloading data files.  Please run data test";
				arg.Result = result;


			} catch (Exception ex) {
				result.Success = false;
				result.Message = ex.Message;

				arg.Result = result;
			}
			
		}
		/// <summary>
		/// Class to inherit from TimerTask and override method Run
		/// </summary>
		public class RenewProgress: TimerTask
		{
			public ZipArchive decompress { get; set; }
			/// <summary>
			/// Overloaded method
			/// </summary>
			public override void Run ()
			{
				if (decompress._per != 0 && decompress._per < decompress._size) {
					int progress = (int)((decompress._per * 100) / decompress._size);
					decompress._worker.ReportProgress (progress);
				}
			}
		}
	}
}