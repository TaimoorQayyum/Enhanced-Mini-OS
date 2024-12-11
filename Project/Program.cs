using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Threading;
using System.Net.Mail;
using System.Net;

namespace EnhancedMiniOS
{
    public class Process
    {
        public int PID { get; }
        public string Name { get; }
        public int BurstTime { get; set; } // Remaining burst time
        public int Priority { get; }

        public Process(int pid, string name, int burstTime, int priority)
        {
            PID = pid;
            Name = name;
            BurstTime = burstTime;
            Priority = priority;
        }
    }

    public class MiniOperatingSystem
    {
        private Queue<Process> readyQueue = new Queue<Process>();
        private int memoryUsed = 0;
        private int memoryLimit = 100000; // Simulated as 100000 MB
        private int pidCounter = 1;

        private const string ConnectionString = "Server=localhost; Database=MiniOS; User ID=root; Password=khan.sql;";

        public MiniOperatingSystem()
        {
            InitializeDatabase();
        }

        // Initialize the database table for processes
        private void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Processes (
                    PID INT PRIMARY KEY,
                    Name VARCHAR(255),
                    BurstTime INT,
                    Priority INT
                );
            ";
                using (var command = new MySqlCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Add process to the database
        private void AddProcessToDatabase(Process process)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Processes (PID, Name, BurstTime, Priority) VALUES (@PID, @Name, @BurstTime, @Priority);";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@PID", process.PID);
                    command.Parameters.AddWithValue("@Name", process.Name);
                    command.Parameters.AddWithValue("@BurstTime", process.BurstTime);
                    command.Parameters.AddWithValue("@Priority", process.Priority);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Remove process from the database
        private void RemoveProcessFromDatabase(int pid)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM Processes WHERE PID = @PID;";
                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@PID", pid);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Create a new process
        public void CreateProcess(string name, int burstTime, int priority = 0)
        {
            try
            {
                if (memoryUsed + burstTime > memoryLimit)
                {
                    Console.WriteLine("Not enough memory to create process.");
                    return;
                }

                Process newProcess = new Process(pidCounter++, name, burstTime, priority);
                readyQueue.Enqueue(newProcess);
                memoryUsed += burstTime;
                AddProcessToDatabase(newProcess); // Add process to the database
                Console.WriteLine($"Process {newProcess.PID}: {newProcess.Name} created with Burst Time {burstTime}ms.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating process: {ex.Message}");
            }
        }

        // Terminate a process
        public void TerminateProcess(int pid)
        {
            try
            {
                var process = readyQueue.FirstOrDefault(p => p.PID == pid);
                if (process != null)
                {
                    readyQueue = new Queue<Process>(readyQueue.Where(p => p.PID != pid));
                    memoryUsed -= process.BurstTime;
                    RemoveProcessFromDatabase(pid); // Remove process from the database
                    Console.WriteLine($"Process {pid} terminated.");
                }
                else
                {
                    Console.WriteLine($"Process {pid} not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error terminating process: {ex.Message}");
            }
        }

        // Display all processes
        public void DisplayProcesses()
        {
            try
            {
                Console.WriteLine("\n--- Process Table ---");
                Console.WriteLine("| PID | Name        | Burst Time (ms) | Priority |");
                Console.WriteLine("-----------------------------------------------");

                foreach (var process in readyQueue)
                {
                    Console.WriteLine($"| {process.PID,-3} | {process.Name,-10} | {process.BurstTime,-15} | {process.Priority,-8} |");
                }

                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine($"Memory Used: {memoryUsed}MB / {memoryLimit}MB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying processes: {ex.Message}");
            }
        }

        public void ProcessesScheduling()
        {
            try
            {
                Console.WriteLine("\n--- Scheduling Menu ---");
                Console.WriteLine("1. First-Come-First-Serve Scheduling");
                Console.WriteLine("2. Priority Scheduling");
                Console.WriteLine("3. Shortest Job First (SJF) Scheduling");
                Console.WriteLine("4. Back to Main Menu");
                Console.Write("Enter your choice: ");

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            ExecuteProcesses();
                            break;
                        case 2:
                            ExecuteProcessesPriority();
                            break;
                        case 3:
                            ExecuteProcessesSJF();
                            break;
                        case 4:
                            Console.WriteLine("Returning to Main Menu...");
                            break;
                        default:
                            Console.WriteLine("Invalid choice. Please select a valid option.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in scheduling menu: {ex.Message}");
            }
        }

        private void ExecuteProcesses()
        {
            try
            {
                Console.WriteLine("\nStarting process execution using FCFS...\n");

                while (readyQueue.Count > 0)
                {
                    var process = readyQueue.Dequeue();
                    Console.WriteLine($"Executing Process {process.PID}: {process.Name}");

                    while (process.BurstTime > 0)
                    {
                        Console.WriteLine($"Process {process.PID} running... Remaining Burst Time: {process.BurstTime}ms");
                        Thread.Sleep(500); // Simulate execution for 500ms
                        process.BurstTime -= 500;

                        if (process.BurstTime <= 0)
                        {
                            Console.WriteLine($"Process {process.PID}: {process.Name} completed.");
                            memoryUsed = Math.Max(0, memoryUsed - process.BurstTime); // Adjust memory usage safely
                            RemoveProcessFromDatabase(process.PID); // Remove completed process from database
                        }
                    }
                }

                Console.WriteLine("\nAll processes executed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during FCFS execution: {ex.Message}");
            }
        }

        private void ExecuteProcessesPriority()
        {
            try
            {
                if (!readyQueue.Any())
                {
                    Console.WriteLine("No processes in the queue to execute.");
                    return;
                }

                Console.WriteLine("\nExecuting Processes by Priority Scheduling...");

                var priorityQueue = readyQueue.OrderByDescending(p => p.Priority).ThenBy(p => p.PID).ToList();
                readyQueue.Clear();

                foreach (var process in priorityQueue)
                {
                    Console.WriteLine($"\nExecuting Process PID: {process.PID}, Name: {process.Name}, Priority: {process.Priority}");
                    Thread.Sleep(process.BurstTime); // Simulate process execution
                    Console.WriteLine($"Process PID: {process.PID} completed.");
                    memoryUsed = Math.Max(0, memoryUsed - process.BurstTime); // Free memory after execution
                }

                Console.WriteLine("\nAll processes executed using Priority Scheduling.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during Priority Scheduling execution: {ex.Message}");
            }
        }

        private void ExecuteProcessesSJF()
        {
            try
            {
                if (!readyQueue.Any())
                {
                    Console.WriteLine("No processes in the queue to execute.");
                    return;
                }

                Console.WriteLine("\nExecuting Processes by Shortest Job First (SJF) Scheduling...");

                var sjfQueue = readyQueue.OrderBy(p => p.BurstTime).ThenBy(p => p.PID).ToList();
                readyQueue.Clear();

                foreach (var process in sjfQueue)
                {
                    Console.WriteLine($"\nExecuting Process PID: {process.PID}, Name: {process.Name}, Burst Time: {process.BurstTime}");
                    Thread.Sleep(process.BurstTime); // Simulate process execution
                    Console.WriteLine($"Process PID: {process.PID} completed.");
                    memoryUsed = Math.Max(0, memoryUsed - process.BurstTime); // Free memory after execution
                }

                Console.WriteLine("\nAll processes executed using Shortest Job First Scheduling.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during SJF Scheduling execution: {ex.Message}");
            }
        }





        public void CreateFile(string name, string content, string password = null)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO Files (FileName, Content, Password) VALUES (@FileName, @Content, @Password)";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FileName", name);
                        command.Parameters.AddWithValue("@Content", content);
                        command.Parameters.AddWithValue("@Password", password);
                        command.ExecuteNonQuery();
                    }
                    Console.WriteLine($"File '{name}' created successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating file: {ex.Message}");
                }
            }
        }

        public void ReadFile(int fileID, string password = null)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Content, Password FROM Files WHERE FileID = @FileID";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FileID", fileID);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPassword = reader["Password"].ToString();
                                if (string.IsNullOrEmpty(storedPassword) || storedPassword == password)
                                {
                                    Console.WriteLine($"File Content: {reader["Content"]}");
                                }
                                else
                                {
                                    Console.WriteLine("Incorrect password.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("File not found.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file: {ex.Message}");
                }
            }
        }

        public void DeleteFile(int fileID)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string query = "DELETE FROM Files WHERE FileID = @FileID";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FileID", fileID);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("File deleted successfully.");
                        }
                        else
                        {
                            Console.WriteLine("File not found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                }
            }
        }

       

        public void ShowDateTime()
        {
            try
            {
                Console.WriteLine($"\nCurrent Date and Time: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying date and time: {ex.Message}");
            }
        }

        public void ShowSystemStats()
        {
            try
            {
                Console.WriteLine("\n--- System Statistics ---");
                Console.WriteLine($"Total Memory: {memoryLimit}MB");
                Console.WriteLine($"Memory Used: {memoryUsed}MB");
                Console.WriteLine($"Number of Processes: {readyQueue.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying system stats: {ex.Message}");
            }
        }

        public void Calculator()
        {
            bool keepRunning = true;

            while (keepRunning)
            {
                try
                {
                    Console.WriteLine("\n--- Calculator Menu ---");
                    Console.WriteLine("1. Addition");
                    Console.WriteLine("2. Subtraction");
                    Console.WriteLine("3. Multiplication");
                    Console.WriteLine("4. Division");
                    Console.WriteLine("5. Power");
                    Console.WriteLine("6. Square Root");
                    Console.WriteLine("7. Logarithm");
                    Console.WriteLine("8. Factorial");
                    Console.WriteLine("9. GCD (Greatest Common Divisor)");
                    Console.WriteLine("10. LCM (Least Common Multiple)");
                    Console.WriteLine("11. Back to Main Menu");

                    Console.Write("Enter your choice: ");
                    if (int.TryParse(Console.ReadLine(), out int calcChoice))
                    {
                        switch (calcChoice)
                        {
                            case 1:
                                PerformCalculation((a, b) => a + b, "Addition");
                                break;
                            case 2:
                                PerformCalculation((a, b) => a - b, "Subtraction");
                                break;
                            case 3:
                                PerformCalculation((a, b) => a * b, "Multiplication");
                                break;
                            case 4:
                                PerformCalculation((a, b) => b != 0 ? a / b : throw new DivideByZeroException(), "Division");
                                break;
                            case 5:
                                PerformPower();
                                break;
                            case 6:
                                PerformSquareRoot();
                                break;
                            case 7:
                                PerformLogarithm();
                                break;
                            case 8:
                                PerformFactorial();
                                break;
                            case 9:
                                PerformGCD();
                                break;
                            case 10:
                                PerformLCM();
                                break;
                            case 11:
                                keepRunning = false;
                                Console.WriteLine("Returning to Main Menu...");
                                break;
                            default:
                                Console.WriteLine("Invalid choice. Please try again.");
                                break;
                        }
                    
                    if (keepRunning)
                    {
                        Console.Write("\nDo you want to perform another operation? (Y/N): ");
                        string input = Console.ReadLine().ToUpper();
                        if (input != "Y")
                        {
                            keepRunning = false;
                            Console.WriteLine("Returning to Main Menu...");
                        }
                    }
                }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a number.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in Calculator: {ex.Message}");
                }
            }
        }

        private void PerformLogarithm()
        {
            try
            {
                Console.Write("Enter the number: ");
                double num = double.Parse(Console.ReadLine());
                Console.Write("Enter the base (default is e for natural log): ");
                string baseInput = Console.ReadLine();

                double logResult;
                if (string.IsNullOrEmpty(baseInput))
                {
                    logResult = Math.Log(num); // Natural log
                }
                else
                {
                    double logBase = double.Parse(baseInput);
                    logResult = Math.Log(num, logBase);
                }

                Console.WriteLine($"The logarithm is: {logResult}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error performing logarithm: {ex.Message}");
            }
        }

        private void PerformFactorial()
        {
            try
            {
                Console.Write("Enter a non-negative integer: ");
                int num = int.Parse(Console.ReadLine());

                if (num < 0)
                {
                    Console.WriteLine("Error: Factorial is not defined for negative numbers.");
                    return;
                }

                long result = 1;
                for (int i = 1; i <= num; i++)
                {
                    result *= i;
                }

                Console.WriteLine($"The factorial of {num} is: {result}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing factorial: {ex.Message}");
            }
        }

        private void PerformGCD()
        {
            try
            {
                Console.Write("Enter the first number: ");
                int num1 = int.Parse(Console.ReadLine());
                Console.Write("Enter the second number: ");
                int num2 = int.Parse(Console.ReadLine());

                int gcd = GCD(num1, num2);
                Console.WriteLine($"The GCD of {num1} and {num2} is: {gcd}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing GCD: {ex.Message}");
            }
        }

        private void PerformLCM()
        {
            try
            {
                Console.Write("Enter the first number: ");
                int num1 = int.Parse(Console.ReadLine());
                Console.Write("Enter the second number: ");
                int num2 = int.Parse(Console.ReadLine());

                int lcm = (num1 * num2) / GCD(num1, num2);
                Console.WriteLine($"The LCM of {num1} and {num2} is: {lcm}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing LCM: {ex.Message}");
            }
        }

        private int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }


        private void PerformCalculation(Func<double, double, double> operation, string operationName)
        {
            try
            {
                Console.Write("Enter first number: ");
                double num1 = double.Parse(Console.ReadLine());
                Console.Write("Enter second number: ");
                double num2 = double.Parse(Console.ReadLine());

                double result = operation(num1, num2);
                Console.WriteLine($"The result of {operationName} is: {result}");
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Error: Division by zero is not allowed.");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing calculation: {ex.Message}");
            }
        }

        private void PerformPower()
        {
            try
            {
                Console.Write("Enter the base number: ");
                double baseNum = double.Parse(Console.ReadLine());
                Console.Write("Enter the exponent: ");
                double exponent = double.Parse(Console.ReadLine());

                double result = Math.Pow(baseNum, exponent);
                Console.WriteLine($"The result of {baseNum}^{exponent} is: {result}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing power calculation: {ex.Message}");
            }
        }

        private void PerformSquareRoot()
        {
            try
            {
                Console.Write("Enter a number: ");
                double num = double.Parse(Console.ReadLine());

                if (num < 0)
                {
                    Console.WriteLine("Error: Cannot calculate the square root of a negative number.");
                    return;
                }

                double result = Math.Sqrt(num);
                Console.WriteLine($"The square root of {num} is: {result}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid number format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing square root calculation: {ex.Message}");
            }
        }

        public void SendEmail(string toAddress, string subject, string body, string attachmentFilePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(toAddress) || !toAddress.Contains("@"))
                {
                    Console.WriteLine("Invalid recipient email address.");
                    return;
                }

                if (string.IsNullOrEmpty(subject))
                {
                    Console.WriteLine("Email subject cannot be empty.");
                    return;
                }
                if (string.IsNullOrEmpty(body))
                {
                    Console.WriteLine("Email body cannot be empty.");
                    return;
                }

                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("tk2104294@gmail.com", "ggmx dzqy mytb tuhb"),
                    EnableSsl = true
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("tk2104294@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                // Add the recipient email
                mailMessage.To.Add(toAddress);

                // Attach file if provided
                if (!string.IsNullOrEmpty(attachmentFilePath))
                {
                    try
                    {
                        if (System.IO.File.Exists(attachmentFilePath))
                        {
                            mailMessage.Attachments.Add(new Attachment(attachmentFilePath));
                            Console.WriteLine($"Attachment added: {attachmentFilePath}");
                        }
                        else
                        {
                            Console.WriteLine($"Attachment file not found: {attachmentFilePath}. Sending email without attachment.");
                        }
                    }
                    catch (Exception attachEx)
                    {
                        Console.WriteLine($"Error attaching file: {attachEx.Message}. Sending email without attachment.");
                    }
                }

                // Send the email
                smtpClient.Send(mailMessage);
                Console.WriteLine("Email sent successfully.");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        private  void PromptReturnToMenu()
        {
            Console.Write("\nPress Enter For Main Menu..... ");
            string input = Console.ReadLine().ToUpper();
            if (input != "Y")
            {
                Console.WriteLine("Returning to Main Menu...");
            }
        }


        class Program
        {
            static void Main(string[] args)
            {
                try
                {
                    MiniOperatingSystem miniOS = new MiniOperatingSystem();
                    Console.WriteLine("Welcome to Enhanced Mini Operating System");

                    bool running = true;
                    while (running)
                    {

                        Console.WriteLine("\n--- Main Menu ---");
                        Console.WriteLine("1. Create Process");
                        Console.WriteLine("2. Terminate Process");
                        Console.WriteLine("3. Display Processes");
                        Console.WriteLine("4. Execute Processes Sheduling ");
                        Console.WriteLine("5. Create File");
                        Console.WriteLine("6. Read File");
                        Console.WriteLine("7. Delete File");
                        Console.WriteLine("8. Show Date and Time");
                        Console.WriteLine("9. Show System Statistics");
                        Console.WriteLine("10. Calculator");
                        Console.WriteLine("11. Send Email");
                        Console.WriteLine("12. Exit");
                        Console.Write("Enter your choice: ");

                        if (int.TryParse(Console.ReadLine(), out int choice))
                        {
                            switch (choice)
                            {
                                case 1:
                                    try
                                    {
                                        Console.Write("Enter Process Name: ");
                                        string processName = Console.ReadLine();
                                        Console.Write("Enter Burst Time (ms): ");
                                        int burstTime = int.Parse(Console.ReadLine());
                                        Console.Write("Enter Priority (optional, default 0): ");
                                        string priorityInput = Console.ReadLine();
                                        int priority = string.IsNullOrEmpty(priorityInput) ? 0 : int.Parse(priorityInput);
                                        miniOS.CreateProcess(processName, burstTime, priority);
                                    }
                                    catch (FormatException ex)
                                    {
                                        Console.WriteLine($"Invalid input for process creation: {ex.Message}");
                                    }
                                    break;

                                case 2:
                                    try
                                    {
                                        Console.Write("Enter Process ID to Terminate: ");
                                        int pid = int.Parse(Console.ReadLine());
                                        miniOS.TerminateProcess(pid);
                                    }
                                    catch (FormatException ex)
                                    {
                                        Console.WriteLine($"Invalid input for terminating process: {ex.Message}");
                                    }
                                    break;

                                case 3:
                                    miniOS.DisplayProcesses();
                                    miniOS.PromptReturnToMenu();
                                    break;

                                case 4:
                                    miniOS.ProcessesScheduling(); 
                                    break;

                                case 5:
                                    try
                                    {
                                        Console.Write("Enter File Name: ");
                                        string fileName = Console.ReadLine();
                                        Console.Write("Enter File Content: ");
                                        string content = Console.ReadLine();
                                        Console.Write("Enter Password (optional): ");
                                        string password = Console.ReadLine();
                                        miniOS.CreateFile(fileName, content, password);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error creating file: {ex.Message}");
                                    }
                                    break;

                                case 6:
                                    try
                                    {
                                        Console.Write("Enter File ID to Read: ");
                                        int fileID = int.Parse(Console.ReadLine());
                                        Console.Write("Enter Password (if required): ");
                                        string filePassword = Console.ReadLine();
                                        miniOS.ReadFile(fileID, filePassword);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error reading file: {ex.Message}");
                                    }
                                    break;

                                case 7:
                                    try
                                    {
                                        Console.Write("Enter File ID to Delete: ");
                                        int deleteFileID = int.Parse(Console.ReadLine());
                                        miniOS.DeleteFile(deleteFileID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error deleting file: {ex.Message}");
                                    }
                                    break;

                                case 8:
                                    miniOS.ShowDateTime();
                                    miniOS.PromptReturnToMenu();
                                    break;

                                case 9:
                                    miniOS.ShowSystemStats();
                                    miniOS.PromptReturnToMenu();
                                    break;

                                case 10:
                                    miniOS.Calculator();
                                    break;

                                case 11:
                                    try
                                    {
                                        Console.Write("Enter recipient email address: ");
                                        string toAddress = Console.ReadLine();

                                        Console.Write("Enter email subject: ");
                                        string subject = Console.ReadLine();

                                        Console.Write("Enter email body: ");
                                        string body = Console.ReadLine();

                                        Console.Write("Enter file path to attach (or press Enter to skip): ");
                                        string attachmentPath = Console.ReadLine();

                                        miniOS.SendEmail(toAddress, subject, body, attachmentPath);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error sending email: {ex.Message}");
                                    }
                                    break;

                                case 12:
                                    running = false;
                                    Console.WriteLine("Exiting Enhanced Mini Operating System. Goodbye!");
                                    break;

                                default:
                                    Console.WriteLine("Invalid choice. Please try again.");
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please enter a valid number.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }
            }
        }
    }
}



