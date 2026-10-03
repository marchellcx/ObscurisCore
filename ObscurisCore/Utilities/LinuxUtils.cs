using LabExtended.Core;
using LabExtended.Utilities;

using NorthwoodLib.Pools;

using System.Diagnostics;

namespace ObscurisCore.Utilities
{
    /// <summary>
    /// Linux-related utility methods.
    /// </summary>
    public static class LinuxUtils
    {
        /// <summary>
        /// Executes the specified command line in a new process and invokes a callback with the combined output upon
        /// completion.
        /// </summary>
        /// <remarks>The method runs the command asynchronously using Bash and captures both standard
        /// output and standard error. The combined output is provided to the callback after the process finishes. If
        /// the command line is null or empty, the method returns immediately without executing the callback.</remarks>
        /// <param name="line">The command line to execute. This parameter cannot be null or empty.</param>
        /// <param name="callback">An action to invoke with the output of the command execution. This parameter cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
        public static void Execute(string line, Action<string> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(line))
                return;

            var sb = StringBuilderPool.Shared.Rent();

            Task.Run(() =>
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"{line}\"";

                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (_, args) =>
                    {
                        if (args.Data != null)
                        {
                            sb.AppendLine(args.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                }
            }).ContinueWithOnMain(tsk => 
            {
                if (tsk.Exception != null)
                {
                    ApiLog.Error(tsk.Exception);
                    
                    StringBuilderPool.Shared.Return(sb);
                    return;
                }

                callback(sb.ToString());

                StringBuilderPool.Shared.Return(sb);
            });
        }

        /// <summary>
        /// Executes the specified command line asynchronously in a new Bash shell process.
        /// </summary>
        /// <remarks>The method starts a background task to run the provided command using Bash. Standard
        /// output and error streams are redirected for logging. If the command line is null or empty, the method does
        /// nothing.</remarks>
        /// <param name="line">The command line to execute in the Bash shell. This parameter cannot be null or empty.</param>
        public static void Execute(string line)
        {
            if (string.IsNullOrEmpty(line)) 
                return;

            Task.Run(() =>
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = "/bin/bash";
                        process.StartInfo.Arguments = $"-c \"{line}\"";

                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;

                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    ApiLog.Error(ex);
                }
            });
        }
    }
}