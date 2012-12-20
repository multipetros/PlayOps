/**
 * PlayOps v1.0
 * Copyright (c) 2012, Petros Kyladitis
 * 
 * An utility to run a program by change the system date to a specified date 
 * and restore to the current date after the started process termination.
 * 
 * This program is free software, distributed under the terms of
 * the FreeBSD License. For licensing details see the "License.txt"
 */
 
using System ;
using System.Diagnostics ;
using System.Runtime.InteropServices ;
using Multipetros ;
using System.IO ;

namespace PlayOps{
	
	class Program{
		
		//struct used for kernel32.dll native system date methods calls
		public struct SystemTime{
		   	public ushort Year ;
		   	public ushort Month ;
 			public ushort DayOfWeek ;
 			public ushort Day ;
  		 	public ushort Hour ;
  			public ushort Minute ;
  			public ushort Second ;
  			public ushort Millisecond ;
		};

		//import windows GetSystenTime method
		[DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
		public extern static void Win32GetSystemTime(ref SystemTime sysTime) ;

		//import windows SetSystemTime method
		[DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
		public extern static bool Win32SetSystemTime(ref SystemTime sysTime) ;
		
		//structs to store date data for date restoring to the real one 
		private static SystemTime realTime ;
		private static SystemTime newTime ;
		
		//ini file, constant parameters
		private const string INI_NAME = "PlayOps.ini" ;
		private const string INI_PROPERTY_RUN = "RUN" ;
		private const string INI_PROPERTY_YEAR = "YEAR" ;
		private const string INI_PROPERTY_MONTH = "MONTH" ;
		private const string INI_PROPERTY_DAY = "DAY" ;

		public static void Main(string[] args){
			Console.Write("PlayOps v.1.0 - Copyright (c) 2012, Petros Kyladitis\n\n") ;
			
			//if passed command line arguments check and handle them
			if(args.Length > 0){
				//on -r or -reset first argument prompt user step by step to create the ini file
				if(args[0].Equals("-r", StringComparison.CurrentCultureIgnoreCase) || args[0].Equals("-reset")){
					Console.WriteLine("Fill the parameters below, to configure the program options") ;
					UserCreateIni() ;
				}
				//on -s or -show argument display the ini contents
				else if(args[0].Equals("-s", StringComparison.CurrentCultureIgnoreCase) || args[0].Equals("-show")){
					ShowIniParameters() ;
					return ;
				}
				//if not of previous arguments, display a usage help message
				else{
					Console.WriteLine("An utility to run a program by change the system date to a specified date and restore to the current date after the started process termination. This program is free software, licensed under the terms of FreeBSD License\n") ;
					Console.WriteLine("Command line parameters:") ;
					Console.WriteLine("-r     : Configure options and run\n-reset : Same as above") ;
					Console.WriteLine("-s     : Displays the program configuration options\n-show  : Same as above") ;
					return ;
				}
			}
			
			//if ini file not exist, prompt user to create it step by step
			if(!File.Exists(INI_NAME)){
				Console.WriteLine(INI_NAME + " not found. Fill the parameters below, to create it.") ;
				UserCreateIni() ;
			}
			
			//read parameters from the ini file
			Props ini = new Props(INI_NAME, true) ;
			string execPath = ini.GetProperty(INI_PROPERTY_RUN) ;
			string yearStr = ini.GetProperty(INI_PROPERTY_YEAR) ;
			string monthStr = ini.GetProperty(INI_PROPERTY_MONTH) ;
			string dayStr = ini.GetProperty(INI_PROPERTY_DAY) ;
			
			//while executable file not exist, prompt user to input and store it to the ini file
			while(execPath == null){
				Console.Write("The file to run does not exist.\nPlease give an existed executable file path: ") ;
				execPath = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_RUN, execPath) ;
			}
			
			//read year value from the ini file, while not exist or is not valid prompt user to input and store it to the ini file
			ushort year ;
			while( (yearStr == null) || (!ushort.TryParse(yearStr, out year)) ){
				Console.Write("There is not a valid value for year.\nPlease give a valid year value: ") ;
				yearStr = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_YEAR, yearStr) ;
			}

			//read month value from the ini file, while not exist or is not valid prompt user to input and store it to the ini file
			ushort month ;
			while( (monthStr == null) || (!ushort.TryParse(monthStr, out month)) || (!ValidMonth(month)) ){
				Console.Write("There is not a valid value for month.\nPlease give a valid month value: ") ;
				monthStr = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_MONTH, monthStr) ;
			}
			
			//read day value from the ini file, while not exist or is not valid prompt user to input and store it to the ini file
			ushort day ;
			while( (dayStr == null) || (!ushort.TryParse(dayStr, out day)) || (!ValidDay(day)) ){
				Console.Write("There is not a valid value for day.\nPlease give a valid day value: ") ;
				dayStr = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_DAY, dayStr) ;
			}
			
			Console.WriteLine("Starting " + execPath + "\n\nWhen the process ends, the system date restore to the current date value and this program will close after 3 seconds. To terminate this program and restore the current date before the running process ends give the command: quit" ) ;
			
			//change system date as user input and start the selected process
			//on executing error display it and exit
			realTime = new SystemTime() ;
			Win32GetSystemTime(ref realTime) ;			
			SetSystemDate(year, month, day) ;
			try{
				RunProc(execPath) ;
			}catch(Exception){
				ResotreSystemDate() ;
				Console.WriteLine("Can not run " + execPath) ;
				return ;
			}
			
			//after the process start make a loop of a user commands prompt
			//if command is quit, stop and close the program after 3 secs
			//normally the program quits if the started process exited by the appropriate handler
			string command = "" ;
			while(!command.Equals("quit", StringComparison.CurrentCultureIgnoreCase)){
				command = Console.ReadLine() ;
			}	
			Exit(3000, "\nPlayOps is closing at 3 seconds...") ;
		}
		
		/// <summary>
		/// Display the current ini contents to the console
		/// </summary>
		private static void ShowIniParameters(){
			Props ini ;
			//if ini file not exist, inform and return
			if(!File.Exists(INI_NAME)){
				Console.WriteLine(INI_NAME + " does not exist") ;
				return ;
			}
			ini = new Props(INI_NAME, false) ;
			string executable = ini.GetProperty(INI_PROPERTY_RUN, true) ;
			string year = ini.GetProperty(INI_PROPERTY_YEAR, true) ;
			string month = ini.GetProperty(INI_PROPERTY_MONTH, true) ;
			string day = ini.GetProperty(INI_PROPERTY_DAY, true) ;
			Console.WriteLine("INI Properties\n--------------") ;
			Console.WriteLine(INI_PROPERTY_RUN + "   : " + executable) ;
			Console.WriteLine(INI_PROPERTY_YEAR + "  : " + year) ;
			Console.WriteLine(INI_PROPERTY_MONTH + " : " + month) ;
			Console.WriteLine(INI_PROPERTY_DAY + "   : " + day) ;
			return ;
		}

		/// <summary>
		/// Create ini file, contacting with user step by step
		/// </summary>
		private static void UserCreateIni(){			
			ushort shortNum ;
			Console.Write("Path to the executable file to run: ") ;
			string executable = Console.ReadLine() ;
			
			Console.Write("Temporary system date setting\nYear: ") ;
			string yearStr = Console.ReadLine() ;
			//while valid year, prompt user to input
			while(!ushort.TryParse(yearStr,out shortNum)){
				Console.Write("Set a valid year value: ") ;
				yearStr = Console.ReadLine() ;
			}
			
			Console.Write("Month: ") ;
			string monthStr = Console.ReadLine() ;
			//while valid month, prompt user to input
			while(!(ushort.TryParse(monthStr, out shortNum) && ValidMonth(shortNum))){
				Console.Write("Set a valid month value: ") ;
				monthStr = Console.ReadLine() ;
			}
			
			Console.Write("Day: ") ;
			string dayStr = Console.ReadLine() ;
			//while valid day, prompt user to input
			while(!(ushort.TryParse(dayStr, out shortNum) && ValidDay(shortNum))){
				Console.Write("Set a valid day value: ") ;
				dayStr = Console.ReadLine() ;
			}
			
			//store user inputs to ini file
			SetIni(executable, yearStr, monthStr, dayStr) ;
		}
		
		/// <summary>
		/// Store ini parameters
		/// </summary>
		/// <param name="executable">Executable path, to run</param>
		/// <param name="year">Year value to set as current</param>
		/// <param name="month">Month value to set as current</param>
		/// <param name="day">Day value to set as current</param>
		/// <returns>True if done, false on write error</returns>
		private static bool SetIni(string executable, string year, string month, string day){
			Props ini = new Props(INI_NAME, false) ;
			ini.SetProperty(INI_PROPERTY_RUN, executable) ;
			ini.SetProperty(INI_PROPERTY_YEAR, year) ;
			ini.SetProperty(INI_PROPERTY_MONTH, month) ;
			ini.SetProperty(INI_PROPERTY_DAY, day) ;
			try{
				ini.Save() ;
				return true ;
			}catch(Exception){
				return false ;
			}
		}
		
		/// <summary>
		/// Check if passed parameter is valid as day
		/// </summary>
		/// <param name="day">Value to check</param>
		/// <returns>True if valid, false if not</returns>
		private static bool ValidDay(ushort day){
			if(day > 31 || day < 1)
				return false ;
			return true ;
		}
		
		/// <summary>
		/// Check if passed parameter is valid as month
		/// </summary>
		/// <param name="month">Value to check</param>
		/// <returns>True if valid, false if not</returns>
		private static bool ValidMonth(ushort month){
			if(month > 12 || month < 1)
				return false ;
			return true ;
		}
		
		/// <summary>
		/// Create a new process of the passed executable file and raise an event handler
		/// to terminate the current program
		/// </summary>
		/// <param name="execPath">Executable file path</param>
		private static void RunProc(string execPath){
			ProcessStartInfo psi = new ProcessStartInfo() ;
			psi.FileName = execPath ;
			
			Process proc = new Process() ;
			proc.StartInfo = psi ;
			proc.EnableRaisingEvents = true ;
			proc.Exited += new EventHandler(proc_Exited) ;
			proc.Start() ;
		}

		//Event handler, fired if started process exit to display
		//message to the user and exit the main program after 3 secs
		static void proc_Exited(object sender, EventArgs e){
			Exit(3000, "\nProcess terminated. PlayOps will close at 3 seconds...") ;
		}
		
		/// <summary>
		/// Restrore date to real curren one, display a message, wait and terminate the program
		/// </summary>
		/// <param name="wait">Waiting time before exit, in milliseconds</param>
		/// <param name="message">Message to display in console</param>
		static void Exit(int wait, string message){
			ResotreSystemDate() ;
			Console.WriteLine(message) ;
			System.Threading.Thread.Sleep(wait) ;
			Environment.Exit(0) ;			
		}
		
		/// <summary>
		/// Set system current date, using native OS call.
		/// </summary>
		/// <param name="year">Year to set</param>
		/// <param name="month">Month to set</param>
		/// <param name="day">Day to set</param>
		private static void SetSystemDate(ushort year, ushort month, ushort day){
			newTime = new SystemTime() ;
			SystemTime oldTime = new SystemTime() ;

			Win32GetSystemTime(ref oldTime) ;
			
			//initialize all values of the new time as the current (old), to change only
			//the date part (year, month, day) and as the time part (hour, min, sec, ms) as is.
			newTime = oldTime ; 
			
			newTime.Year = year ;
			newTime.Month = month ;
			newTime.Day = day ;			
			Win32SetSystemTime(ref newTime) ;
		}
		
		/// <summary>
		/// Restore the system date to the date before changing it, using native OS calls
		/// </summary>
		private static void ResotreSystemDate(){
			SystemTime currentTime = new SystemTime() ;
			Win32GetSystemTime(ref currentTime) ;
			
			//find differences between the current date and the setted date to added
			//to the real date. etc. if a day passes after the prior changing
			ushort yearDiff = (ushort) (currentTime.Year - newTime.Year) ;
			ushort monthDiff = (ushort) (currentTime.Month - newTime.Month) ;
			ushort dayDiff = (ushort) (currentTime.Day - newTime.Day) ;
			
			//set current date values to the values before the prior changing
			//and add the previous differences
			currentTime.Year = (ushort) (realTime.Year + yearDiff) ;
			currentTime.Month = (ushort) (realTime.Month + monthDiff) ;
			currentTime.Day = (ushort) (realTime.Day + dayDiff) ;
			
			Win32SetSystemTime(ref currentTime) ;
		}
	}
}