using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VideoContactSheetMaker {
	class Program {
		static int Main(string[] args) {
			Console.OutputEncoding = Encoding.UTF8;
			if (!Utils.FfFilesExist) {
				Console.WriteLine(Properties.Resources.FFmpegFFprobeIsMissing);
				return -1;
			}

			var cp = new ConsoleProgram();
			var result = cp.Run(args);

			return result;
		}

		class ConsoleProgram {
			static readonly char PATHS_SEP = Path.PathSeparator;

			readonly List<InputFile> IncludeFolders = new List<InputFile>();
			bool Recursive;
			string OutputFolder;
			IProfile Profile = new DefaultProfile3();

			ContactSheetMaker cs;


			public int Run(string[] args) {
				try {
					ParseCommandLine(args);
					if (IncludeFolders.Count == 0) {
						Console.WriteLine(Properties.Resources.MissingInputFileFolder);
						return 1;
					}
					EnsureFFFilesExist();
					Console.WriteLine(Environment.NewLine + Environment.NewLine);
					DoWork();
				}
				catch (ParseException ex) {
					PrintHelp();
					Console.WriteLine();
					Console.WriteLine(string.Format(Properties.Resources.InvalidArgument, ex.Message));
					return 1;
				}
				catch (Exception ex) {
					Console.WriteLine();
					ConsoleHelpers.WriteException(ex);
					return 1;
				}
				return 0;
			}

			void DoWork() {
				cs = new ContactSheetMaker(Profile, IncludeFolders, Recursive, OutputFolder);
				cs.CreateThumbnails();
			}

			static void PrintHelp() {
				Console.WriteLine(Properties.Resources.AvailableCommands);
				Console.WriteLine();
				foreach (var info in helpInfos) {
					var arg = info.Option;
					if (info.Args != null)
						arg = arg + " " + info.Args;
					Console.WriteLine("  {0,-12}   {1}", arg, string.Format(info.ArgsDescription, PATHS_SEP));
				}
				Console.WriteLine();
			}

			static void EnsureFFFilesExist() {
				if (!File.Exists(Utils.FfprobePath) && !File.Exists(Utils.FfprobePath + ".exe")) {
					throw new ParseException(string.Format(Properties.Resources.MissingFile, Utils.FFprobeExecutableName));
				}
				if (!File.Exists(Utils.FfmpegPath) && !File.Exists(Utils.FfmpegPath + ".exe")) {
					throw new ParseException(string.Format(Properties.Resources.MissingFile, Utils.FFmpegExecutableName));
				}
			}

			readonly struct HelpInfo {
				public string Option { get; }
				public string Args { get; }
				public string ArgsDescription { get; }
				public HelpInfo(string option, string args, string argsDescription) {
					Option = option;
					Args = args;
					ArgsDescription = argsDescription;
				}
			}
			static readonly HelpInfo[] helpInfos = {
			new HelpInfo("-i", Properties.Resources.Path, Properties.Resources.IncludeAFileOrFolder),
			new HelpInfo("-p", Properties.Resources.Path, Properties.Resources.ProfilePath),
			new HelpInfo("-r", string.Empty,Properties.Resources.RecursiveAppliesToIncludedFolders),
			new HelpInfo("-o", Properties.Resources.Path,Properties.Resources.OutputFolder),
		};

			void ParseCommandLine(string[] args) {
				if (args.Length == 0)
					throw new ParseException(Properties.Resources.MissingArgs);

				for (int i = 0; i < args.Length; i++) {
					var arg = args[i];
					var next = i + 1 < args.Length ? args[i + 1] : null;
					if (arg.Length == 0)
						continue;

					if (arg[0] == '-') {
						switch (arg) {

						case "-r":
							Recursive = true;
							break;

						case "-p":
							if (next == null)
								throw new ParseException(Properties.Resources.TheProfilePathIsInvalid);
							IProfile profile = null;
							if (File.Exists(Path.GetFullPath(next))) {
								if (!string.IsNullOrEmpty(next)) {
									try {
										var x = new System.Xml.Serialization.XmlSerializer(typeof(SerializableProfile));
										using (var fs = File.OpenRead(next))
											profile = (SerializableProfile)x.Deserialize(fs);
									}
									catch (Exception e) {
										Console.WriteLine(e);
										throw new ParseException(Properties.Resources.TheProfilePathIsInvalid);
									}
								}
							}
							else {
								if (next.Equals("default1", StringComparison.OrdinalIgnoreCase))
									profile = new DefaultProfile1();
								else if (next.Equals("default2", StringComparison.OrdinalIgnoreCase))
									profile = new DefaultProfile2();
								else if (next.Equals("default3", StringComparison.OrdinalIgnoreCase))
									profile = new DefaultProfile3();
								else
									throw new ParseException(Properties.Resources.TheProfilePathIsInvalid);
							}
							Profile = profile ?? new DefaultProfile1();
							i++;
							break;

						case "-o":
							if (next == null)
								throw new ParseException(Properties.Resources.MissingOutputFolderArgument);
							OutputFolder = Path.GetFullPath(next);
							if (!Directory.Exists(OutputFolder))
								Directory.CreateDirectory(OutputFolder);
							i++;
							break;

						case "-i":
							if (next == null)
								throw new ParseException(Properties.Resources.MissingIncludeArgument);

							//is it an URL?
							var uri = new Uri(next);
							if (!uri.IsFile) {
								IncludeFolders.Add(new InputFile(next, InputFileType.Url));
							}
							else {	
								var path = Path.GetFullPath(next);
								var isDir = Directory.Exists(path);
								if (!isDir && !File.Exists(path)) {
									throw new ParseException(string.Format(Properties.Resources.ThePathDoesNotExist, path));
								}
								IncludeFolders.Add(new InputFile(path, isDir ? InputFileType.Directory : InputFileType.File));
							}
							i++;
							break;
						}
					}
					else
						throw new ParseException(string.Format(Properties.Resources.UnknownArgument, arg));

				}
			}
		}
	}
	sealed class ParseException : Exception {
		public ParseException(string message)
			: base(message) {
		}
	}

}
