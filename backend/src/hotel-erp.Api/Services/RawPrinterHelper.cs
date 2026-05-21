using System.Runtime.InteropServices;

namespace hotel_erp.Api.Services
{
    public static class RawPrinterHelper
    {
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumPrinters(int Flags, string? Name, int Level, IntPtr pPrinterEnum, int cbBuf, out int pcbNeeded, out int pcReturned);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PRINTER_INFO_2
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pServerName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pPrinterName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pShareName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pPortName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDriverName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pComment;
            [MarshalAs(UnmanagedType.LPWStr)] public string pLocation;
            public IntPtr pDevMode;
            [MarshalAs(UnmanagedType.LPWStr)] public string pSepFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pPrintProcessor;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDatatype;
            [MarshalAs(UnmanagedType.LPWStr)] public string pParameters;
            public IntPtr pSecurityDescriptor;
            public int Attributes;
            public int Priority;
            public int DefaultPriority;
            public int StartTime;
            public int UntilTime;
            public int Status;
            public int cJobs;
            public int AveragePPM;
        }

        private const int PRINTER_ENUM_LOCAL = 0x00000002;
        private const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

        public static string[] GetInstalledPrinters()
        {
            const int level = 2;
            int cbNeeded = 0, cReturned = 0;

            EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS, null, level, IntPtr.Zero, 0, out cbNeeded, out cReturned);

            if (cbNeeded == 0)
                return Array.Empty<string>();

            var pAddr = Marshal.AllocHGlobal(cbNeeded);
            try
            {
                if (!EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS, null, level, pAddr, cbNeeded, out cbNeeded, out cReturned))
                    return Array.Empty<string>();

                var printers = new List<string>();
                var ptr = pAddr;
                var size = Marshal.SizeOf<PRINTER_INFO_2>();

                for (int i = 0; i < cReturned; i++)
                {
                    var info = Marshal.PtrToStructure<PRINTER_INFO_2>(ptr);
                    printers.Add(info.pPrinterName);
                    ptr += size;
                }

                return printers.ToArray();
            }
            finally
            {
                Marshal.FreeHGlobal(pAddr);
            }
        }

        public static (bool Success, string Message) TestPrinterConnection(string printerName)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            {
                var err = Marshal.GetLastWin32Error();
                var msg = err switch
                {
                    1801 => $"Nombre de impresora '{printerName}' no encontrado. Verifique el nombre en Configuración.",
                    1802 => $"Impresora '{printerName}' no está disponible.",
                    5 => $"Permiso denegado. Ejecute la aplicación como Administrador.",
                    _ => $"Error {err} al abrir impresora '{printerName}'."
                };
                return (false, msg);
            }

            ClosePrinter(hPrinter);
            return (true, "Conexión exitosa. Impresora lista.");
        }

        public static bool SendBytesToPrinter(string printerName, byte[] data)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            {
                var err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"No se pudo abrir la impresora '{printerName}'. Error: {err}");
            }

            try
            {
                var docInfo = new DOCINFO
                {
                    pDocName = "Factura Hotel",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                    throw new InvalidOperationException("StartDocPrinter falló");

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        throw new InvalidOperationException("StartPagePrinter falló");

                    try
                    {
                        var pData = Marshal.AllocHGlobal(data.Length);
                        try
                        {
                            Marshal.Copy(data, 0, pData, data.Length);
                            if (!WritePrinter(hPrinter, pData, data.Length, out var written))
                                throw new InvalidOperationException($"WritePrinter falló. Escritos: {written} de {data.Length}");
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pData);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }

            return true;
        }
    }
}

