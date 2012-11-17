using System ;
using System.Diagnostics ;
using System.Runtime.InteropServices ;
using Multipetros ;
using System.IO ;

namespace PlayOps{
	
	class Program{
		
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

		[DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
		public extern static void Win32GetSystemTime(ref SystemTime sysTime) ;

		[DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
		public extern static bool Win32SetSystemTime(ref SystemTime sysTime) ;
		
		private static SystemTime realTime ;
		private static SystemTime newTime ;
		
		private const string INI_NAME = "PlayOps.ini" ;
		private const string INI_PROPERTY_RUN = "RUN" ;
		private const string INI_PROPERTY_YEAR = "YEAR" ;
		private const string INI_PROPERTY_MONTH = "MONTH" ;
		private const string INI_PROPERTY_DAY = "DAY" ;

		public static void Main(string[] args){
			Console.Write("PlayOps v.1.0 - Copyright (c) 2012, Petros Kyladitis\n\n") ;
			
			if(args.Length > 0){
				if(args[0].Equals("-r", StringComparison.CurrentCultureIgnoreCase) || args[0].Equals("-reset")){
					Console.WriteLine("Fill the parameters below, to configure the program options") ;
					UserCreateIni() ;
				}
				else if(args[0].Equals("-s", StringComparison.CurrentCultureIgnoreCase) || args[0].Equals("-show")){
					ShowIniParameters() ;
					return ;
				}
				else{
					Console.WriteLine("An utility to run a program by change the system date to a specified date and restore to the current date after the started process termination. This program is free software, licensed under the terms of FreeBSD License\n") ;
					Console.WriteLine("Command line parameters:") ;
					Console.WriteLine("-r     : Configure options and run\n-reset : Same as above") ;
					Console.WriteLine("-s     : Displays the program configuration options\n-show  : Same as above") ;
					return ;
				}
			}
			
			if(!File.Exists(INI_NAME)){
				Console.WriteLine(INI_NAME + " not found. Fill the parameters below, to create it.") ;
				UserCreateIni() ;
			}
			
			Props ini = new Props(INI_NAME, true) ;
			string execPath = ini.GetProperty(INI_PROPERTY_RUN) ;
			string yearStr = ini.GetProperty(INI_PROPERTY_YEAR) ;
			string monthStr = ini.GetProperty(INI_PROPERTY_MONTH) ;
			string dayStr = ini.GetProperty(INI_PROPERTY_DAY) ;
			
			while(execPath == null){
				Console.Write("The file to run does not exist.\nPlease give an existed executable file path: ") ;
				execPath = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_RUN, execPath) ;
			}
			
			ushort year ;
			while( (yearStr == null) || (!ushort.TryParse(yearStr, out year)) ){
				Console.Write("There is not a valid value for year.\nPlease give a valid year value: ") ;
				yearStr = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_YEAR, yearStr) ;
			}
			
			ushort month ;
			while( (monthStr == null) || (!ushort.TryParse(monthStr, out month)) || (!ValidMonth(month)) ){
				Console.Write("There is not a valid value for month.\nPlease give a valid month value: ") ;
				monthStr = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_MONTH, monthStr) ;
			}
			
			ushort day ;
			while( (dayStr == null) || (!ushort.TryParse(dayStr, out day)) || (!ValidDay(day)) ){
				Console.Write("There is not a valid value for day.\nPlease give a valid day value: ") ;
				dayStr = Console.ReadLine() ;
				ini.SetProperty(INI_PROPERTY_DAY, dayStr) ;
			}
			
			Console.WriteLine("Starting " + execPath + "\n\nWhen the process ends, the system date restore to the current date value and this program will close after 3 seconds. To terminate this program and restore the current date before the running process ends give the command: quit" ) ;
			
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
			
			string command = "" ;
			while(!command.Equals("quit", StringComparison.CurrentCultureIgnoreCase)){
				command = Console.ReadLine() ;
			}	
			Exit(3000, "\nPlayOps is closing at 3 seconds...") ;
		}
		
		private static void ShowIniParameters(){
			Props ini ;
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

		private static void UserCreateIni(){			
			ushort shortNum ;
			Console.Write("Path to the executable file to run: ") ;
			string executable = Console.ReadLine() ;
			
			Console.Write("Temporary system date setting\nYear: ") ;
			string yearStr = Console.ReadLine() ;
			while(!ushort.TryParse(yearStr,out shortNum)){
				Console.Write("Set a valid year value: ") ;
				yearStr = Console.ReadLine() ;
			}
			
			Console.Write("Month: ") ;
			string monthStr = Console.ReadLine() ;
			while(!(ushort.TryParse(monthStr, out shortNum) && ValidMonth(shortNum))){
				Console.Write("Set a valid month value: ") ;
				monthStr = Console.ReadLine() ;
			}
			
			Console.Write("Day: ") ;
			string dayStr = Console.ReadLine() ;
			while(!(ushort.TryParse(dayStr, out shortNum) && ValidDay(shortNum))){
				Console.Write("Set a valid day value: ") ;
				dayStr = Console.ReadLine() ;
			}
			
			SetIni(executable, yearStr, monthStr, dayStr) ;
		}
		
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
		
		private static bool ValidDay(ushort day){
			if(day > 31 || day < 1)
				return false ;
			return true ;
		}
		
		private static bool ValidMonth(ushort month){
			if(month > 12 || month < 1)
				return false ;
			return true ;
		}
		
		private static void RunProc(string execPath){
			ProcessStartInfo psi = new ProcessStartInfo() ;
			psi.FileName = execPath ;
			
			Process proc = new Process() ;
			proc.StartInfo = psi ;
			proc.EnableRaisingEvents = true ;
			proc.Exited += new EventHandler(proc_Exited) ;
			proc.Start() ;
		}

		static void proc_Exited(object sender, EventArgs e){
			Exit(3000, "\nProcess terminated. PlayOps will close at 3 seconds...") ;
		}
		
		static void Exit(int wait, string message){
			ResotreSystemDate() ;
			Console.WriteLine(message) ;
			System.Threading.Thread.Sleep(wait) ;
			Environment.Exit(0) ;			
		}
		
		private static void SetSystemDate(ushort year, ushort month, ushort day){
			newTime = new SystemTime() ;
			SystemTime oldTime = new SystemTime() ;

			Win32GetSystemTime(ref oldTime) ;			
			newTime = oldTime ;		
			
			newTime.Year = year ;
			newTime.Month = month ;
			newTime.Day = day ;			
			Win32SetSystemTime(ref newTime) ;
		}
		
		private static void ResotreSystemDate(){
			SystemTime currentTime = new SystemTime() ;
			Win32GetSystemTime(ref currentTime) ;
			
			ushort yearDiff = (ushort) (currentTime.Year - newTime.Year) ;
			ushort monthDiff = (ushort) (currentTime.Month - newTime.Month) ;
			ushort dayDiff = (ushort) (currentTime.Day - newTime.Day) ;
			
			currentTime.Year = (ushort) (realTime.Year + yearDiff) ;
			currentTime.Month = (ushort) (realTime.Month + monthDiff) ;
			currentTime.Day = (ushort) (realTime.Day + dayDiff) ;
			
			Win32SetSystemTime(ref currentTime) ;
		}
	}
}