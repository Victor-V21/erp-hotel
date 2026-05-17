using System.Text;
using hotel_erp.Domain.Entities;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Infrastructure.Services
{
    public class EscPosService
    {
        private static readonly Encoding Enc = GetEncoder();
        private static Encoding GetEncoder()
        {
            try { return Encoding.GetEncoding("IBM858"); }
            catch { return Encoding.UTF8; }
        }

        // Comandos ESC/POS
        private static readonly byte[] Init = { 0x1B, 0x40 };
        private static readonly byte[] Center = { 0x1B, 0x61, 0x01 };
        private static readonly byte[] Left = { 0x1B, 0x61, 0x00 };
        private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
        private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };
        private static readonly byte[] FontA = { 0x1B, 0x4D, 0x00 };
        private static readonly byte[] FontB = { 0x1B, 0x4D, 0x01 };
        private static readonly byte[] CutPartial = { 0x1D, 0x56, 0x42, 0x00 };
        private static readonly byte[] CutFull = { 0x1B, 0x69 };
        private static readonly byte[] LineSpacing0 = { 0x1B, 0x33, 0x18 }; // 24/144in
        private static readonly byte[] LineSpacing1 = { 0x1B, 0x32 };        // default
        private static readonly byte[] LineSpacing2 = { 0x1B, 0x33, 0x48 }; // 72/144in

        private const string CenterPrefix = "\x01";

        private byte[] Feed(int lines) => new byte[] { 0x1B, 0x64, (byte)lines };
        private byte[] Bytes(string text) => Enc.GetBytes(SanitizeText(text) + "\n");
        private static string SanitizeText(string text)
        {
            return text
                .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i')
                .Replace('ó', 'o').Replace('ú', 'u')
                .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I')
                .Replace('Ó', 'O').Replace('Ú', 'U')
                .Replace('ñ', 'n').Replace('Ñ', 'N')
                .Replace('ü', 'u').Replace('Ü', 'U')
                .Replace('¡', '!').Replace('¿', '?');
        }
        private byte[] Combine(params byte[][] arrays)
        {
            var ms = new MemoryStream();
            foreach (var a in arrays) ms.Write(a);
            return ms.ToArray();
        }

        private static string Ctr(string text, int w)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var pad = (w - text.Length) / 2;
            return pad > 0 ? new string(' ', pad) + text : text;
        }

        private static string MLine(string label, string value, int labelCol, int valCol, string prefix = "L ")
        {
            return label.PadRight(labelCol) + (prefix + value).PadLeft(valCol);
        }

        private static string Val(string v, int col) => ("L " + v).PadLeft(col);
        private static string NegVal(string v, int col) => ("-L " + v).PadLeft(col);

        /// <summary>
        /// Genera líneas de texto PLAIN para la factura.
        /// Usado TANTO por preview como por impresión ESC/POS.
        /// </summary>
        public List<string> BuildLines(Invoice invoice, BusinessSettings settings, string? guestName, string? checkIn, string? checkOut, string tipoLabel, bool includePago)
        {
            var lines = new List<string>();
            var w = settings?.PrintWidth > 0 ? settings.PrintWidth : 46;
            var labelCol = w - 12;
            var valCol = 12;
            var ml = settings?.MarginLeft ?? 0;
            var margin = ml > 0 ? new string(' ', ml) : "";
            var sepChar = string.IsNullOrEmpty(settings?.SeparatorChar) ? "-" : settings.SeparatorChar[..1];
            var align = settings?.HeaderAlign ?? "center";
            var negName = settings?.BusinessName ?? "HOTEL";
            var negRtn = settings?.RTN ?? "";
            var negAddr = settings?.Address ?? "";
            var negTel = settings?.Phone ?? "";
            var negEmail = settings?.Email ?? "";
            var negFooter = settings?.Footer ?? "¡Gracias por su preferencia!";

            void L(string s) => lines.Add(margin + s);
            void LC(string s) => lines.Add(CenterPrefix + s);
            void Sep() => L(new string(sepChar[0], w));
            void Empty() => L("");

            var correlativeParts = invoice.CorrelativeNumber.Split('-');
            var initialRange = correlativeParts.Length >= 3
                ? $"{correlativeParts[0]}-{correlativeParts[1]}-{correlativeParts[2]}-00000001"
                : invoice.CAI.InitialRange;

            // Logo
            if (settings?.ShowLogo != false && !string.IsNullOrEmpty(settings?.LogoBase64))
            {
                // El logo se maneja en frontend (preview) y en RasterImage (print). No se agrega texto aquí.
            }

            // Header
            if (settings?.ShowHeader != false)
            {
                LC(negName);
                LC($"RTN: {negRtn}");
                LC($"{negAddr} | Tel: {negTel}");
                LC($"Email: {negEmail}");
                Sep();
            }

            // Tipo de documento
            LC(tipoLabel);
            LC("FACTURA");
            Sep();

            // Información fiscal
            if (settings?.ShowFiscal != false)
            {
                L($"CAI: {invoice.CAI.CAINumber}");
                L($"Factura: {invoice.CorrelativeNumber}");
                var finalParts = invoice.CAI.FinalRange.Split('-');
                var finalRange = finalParts.Length == 4
                    ? $"{finalParts[0]}-{finalParts[1]}-{finalParts[2]}-{finalParts[3].PadLeft(8, '0')[..8]}"
                    : invoice.CAI.FinalRange;
                L($"Rango Autorizado: {initialRange} al {finalRange}");
                L($"Fecha Límite de Emisión: {invoice.CAI.DueDate:dd/MM/yyyy}");
                L($"Fecha de Emisión: {invoice.InvoiceDate:dd/MM/yyyy HH:mm}");
                Sep();
            }

            // Huésped + Exoneración (SAR Art. 10)
            if (settings?.ShowGuest != false)
            {
                L($"{guestName ?? invoice.CustomerName}");
                if (checkIn != null) L($"Ingreso: {checkIn}  Salida: {checkOut}");
                L($"RTN: {invoice.RTNCliente ?? "C/F"}");

                // Dos columnas para ahorrar espacio
                var half = w / 2;
                L($"{"O.C. Exenta:".PadRight(half)}{"R. Exonerado:".PadRight(half)}");
                L($"{"Reg. SAG:".PadRight(half)}{"".PadRight(half)}");
                Sep();
            }

            // Items
            if (settings?.ShowItems != false)
            {
                L("Item".PadRight(labelCol) + " L  Total".PadLeft(valCol));
                foreach (var item in invoice.InvoiceItems)
                {
                    if (item.Description.Contains('|'))
                    {
                        var parts = item.Description.Split('|');
                        var firstLine = parts[0].Trim();
                        var secondLine = parts[1].Trim();
                        L(Truncate(firstLine, labelCol).PadRight(labelCol) + Val(item.UnitPrice.ToString("F2"), valCol));
                        L(Truncate(secondLine, labelCol).PadRight(labelCol) + Val(item.LineTotal.ToString("F2"), valCol));
                    }
                    else
                    {
                        L(Truncate(item.Description, labelCol).PadRight(labelCol) + Val(item.LineTotal.ToString("F2"), valCol));
                    }
                }
                Sep();
            }

            // Totales
            if (settings?.ShowTotals != false)
            {
                L(MLine("Subtotal:", invoice.SubTotal.ToString("F2"), labelCol, valCol));
                if (invoice.DiscountsAmount > 0)
                {
                    var pct = invoice.InvoiceItems?.Max(i => i.DiscountPercentage) ?? 0;
                    L(MLine($"Descuento ({pct}%):", invoice.DiscountsAmount.ToString("F2"), labelCol, valCol, "-L "));
                }
                L(MLine("ISV 15%:", invoice.ISVAmount.ToString("F2"), labelCol, valCol));
                L(MLine("Tasa Turística 4%:", invoice.TouristTaxAmount.ToString("F2"), labelCol, valCol));
                Empty();
                LC("TOTAL A PAGAR:");
                LC("L " + invoice.TotalAmount.ToString("F2"));
                Sep();
            }

            // Pago
            if (includePago && settings?.ShowPayment != false)
            {
                L($"Pago: {invoice.PaymentMethod ?? "Efectivo"}");
                if (invoice.CashReceived.HasValue)
                    L($"Recibido: L {invoice.CashReceived:F2}");
                if (invoice.CashChange.HasValue && invoice.CashChange > 0)
                    L($"Cambio: L {invoice.CashChange:F2}");
                Sep();
            }

            // Footer
            if (settings?.ShowFooter != false)
            {
                LC($"Son: {NumeroALetras(invoice.TotalAmount)}");
                Sep();
                LC(negFooter);
                LC("La factura es beneficio de todos, ¡exíjala!");
                Empty();
            }

            return lines;
        }

        public string GeneratePreviewText(Invoice invoice, BusinessSettings settings, string? guestName, string? checkIn, string? checkOut)
        {
            var lines = BuildLines(invoice, settings, guestName, checkIn, checkOut, "ORIGINAL: CLIENTE", true);
            var w = settings?.PrintWidth > 0 ? settings.PrintWidth : 46;
            var ml = settings?.MarginLeft ?? 0;
            var margin = ml > 0 ? new string(' ', ml) : "";
            var processed = lines.Select(line =>
                line.StartsWith(CenterPrefix)
                    ? margin + Ctr(line.Replace(CenterPrefix, ""), w)
                    : line
            );
            return string.Join("\n", processed);
        }

        public byte[] GenerateTwoCopyInvoice(Invoice invoice, BusinessSettings settings, string? guestName, string? checkIn, string? checkOut)
        {
            byte[] BuildTicket(string tipoLabel, bool includePago, bool includeLogo)
            {
                var lines = BuildLines(invoice, settings, guestName, checkIn, checkOut, tipoLabel, includePago);
                var ms = new MemoryStream();

                void W(byte[] d) => ms.Write(d);
                void Ln(string text) => W(Bytes(text));

                W(Init);

                // Font selection
                var fontIsA = settings?.PrintFontSize == "normal";
                if (fontIsA) W(FontA); else W(FontB);

                // Line spacing
                var spacing = settings?.PrintLineSpacing ?? 1;
                if (spacing == 0) W(LineSpacing0);
                else if (spacing == 2) W(LineSpacing2);
                else W(LineSpacing1);

                // Logo (solo en original)
                if (includeLogo && !string.IsNullOrEmpty(settings?.LogoBase64))
                {
                    try
                    {
                        var logoBytes = Convert.FromBase64String(settings.LogoBase64.Split(',').LastOrDefault() ?? settings.LogoBase64);
                        var maxH = settings.PrintLogoHeight > 0 ? settings.PrintLogoHeight : 40;
                        W(Center);
                        W(RasterImage(logoBytes, Math.Min(200, (settings?.PrintWidth ?? 46) * 4), maxH));
                        W(Left);
                    }
                    catch { }
                }

                // Imprimir cada línea tal cual (BuildLines ya aplica centrado con espacios via Ctr())
                foreach (var rawLine in lines)
                {
                    if (rawLine.StartsWith(CenterPrefix))
                    {
                        var clean = rawLine.Replace(CenterPrefix, "");
                        W(Center);
                        if (clean == "TOTAL A PAGAR:" || clean == "FACTURA") W(BoldOn);
                        Ln(clean);
                        if (clean == "TOTAL A PAGAR:" || clean == "FACTURA") W(BoldOff);
                        W(Left);
                    }
                    else
                    {
                        var trimmed = rawLine.TrimStart();
                        if (trimmed.StartsWith("Item") && rawLine.Contains("L  Total"))
                        {
                            W(BoldOn); Ln(rawLine); W(BoldOff);
                        }
                        else
                        {
                            Ln(rawLine);
                        }
                    }
                }

                return ms.ToArray();
            }

            var original = BuildTicket("ORIGINAL: CLIENTE", true, true);
            var copia = BuildTicket("COPIA: OBLIGADO TRIBUTARIO EMISOR", false, false);

            return Combine(original, Feed(1), CutPartial, copia, Feed(8), CutFull);
        }

        private static byte[] RasterImage(byte[] imageBytes, int maxWidth, int maxHeight)
        {
            try
            {
                using var ms = new MemoryStream(imageBytes);
                using var img = System.Drawing.Image.FromStream(ms);
                using var bmp = new System.Drawing.Bitmap(img);

                var ratioW = (double)maxWidth / bmp.Width;
                var ratioH = (double)maxHeight / bmp.Height;
                var ratio = Math.Min(ratioW, ratioH);
                var w = Math.Max(1, (int)(bmp.Width * ratio));
                var h = Math.Max(1, (int)(bmp.Height * ratio));
                using var resized = new System.Drawing.Bitmap(bmp, w, h);

                var msOut = new MemoryStream();
                msOut.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00 });

                var widthBytes = (w + 7) / 8;
                msOut.Write(BitConverter.GetBytes((ushort)widthBytes));
                msOut.Write(BitConverter.GetBytes((ushort)h));

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x += 8)
                    {
                        byte b = 0;
                        for (int bit = 0; bit < 8; bit++)
                        {
                            if (x + bit < w)
                            {
                                var px = resized.GetPixel(x + bit, y);
                                if (px.GetBrightness() < 0.5)
                                    b |= (byte)(0x80 >> bit);
                            }
                        }
                        msOut.WriteByte(b);
                    }
                }
                return msOut.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Genera bytes ESC/POS para la regla de calibracion.
        /// Usa los mismos comandos que una factura real (Init + Font B + LineSpacing).
        /// </summary>
        public byte[] BuildRulerData(BusinessSettings settings, int rulerWidth = 100)
        {
            var sb = new StringBuilder();

            // Linea 1: regla numerada 1.....+....2.....+....3.....+....4.....+....5...
            for (int i = 0; i < rulerWidth; i++)
            {
                if (i > 0 && i % 10 == 0) sb.Append((i / 10 % 10).ToString()[0]);
                else if (i % 5 == 0) sb.Append('+');
                else sb.Append('.');
            }
            sb.AppendLine();

            // Linea 2: separador
            sb.AppendLine(new string('=', rulerWidth));

            // Linea 3: mayusculas
            sb.AppendLine(RepeatToWidth("ABCDEFGHIJKLMNOPQRSTUVWXYZ", rulerWidth));

            // Linea 4: minusculas
            sb.AppendLine(RepeatToWidth("abcdefghijklmnopqrstuvwxyz", rulerWidth));

            // Linea 5: numeros
            sb.AppendLine(RepeatToWidth("0123456789", rulerWidth));

            // Linea 6: caracteres especiales
            sb.AppendLine(RepeatToWidth("!@#$%^&*()_+-=[]{}|;:,./<>?", rulerWidth));

            var lines = sb.ToString();
            return Combine(Init, FontB, LineSpacing0, Bytes(lines), Feed(4), CutFull);
        }

        private static string RepeatToWidth(string pattern, int width)
        {
            var sb = new StringBuilder(width);
            while (sb.Length < width)
                sb.Append(pattern);
            return sb.ToString()[..width];
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..Math.Max(3, max - 3)] + "...";

        private static string NumeroALetras(decimal numero)
        {
            var entero = (long)Math.Floor(numero);
            var centavos = ((int)Math.Round((numero - entero) * 100)).ToString("00");
            return entero == 0 ? $"CERO LEMPIRAS CON {centavos}/100" : $"{EnLetras(entero)} LEMPIRAS CON {centavos}/100";
        }

        private static string EnLetras(long n) => n switch
        {
            0 => "CERO", 100 => "CIEN", < 100 => Decenas(n), < 1000 => Centenas(n), < 1000000 => Miles(n), _ => Millones(n)
        };
        private static string Unidades(long n) => n switch
        {
            1 => "UNO", 2 => "DOS", 3 => "TRES", 4 => "CUATRO", 5 => "CINCO",
            6 => "SEIS", 7 => "SIETE", 8 => "OCHO", 9 => "NUEVE", _ => ""
        };
        private static string Decenas(long n) => n switch
        {
            10 => "DIEZ", 11 => "ONCE", 12 => "DOCE", 13 => "TRECE", 14 => "CATORCE",
            15 => "QUINCE", 16 => "DIECISEIS", 17 => "DIECISIETE", 18 => "DIECIOCHO", 19 => "DIECINUEVE",
            20 => "VEINTE", >= 21 and < 30 => $"VEINTI{Unidades(n - 20)}",
            30 => "TREINTA", 40 => "CUARENTA", 50 => "CINCUENTA",
            60 => "SESENTA", 70 => "SETENTA", 80 => "OCHENTA", 90 => "NOVENTA",
            _ when n > 30 => $"{Decenas((n / 10) * 10)} Y {Unidades(n % 10)}", _ => Unidades(n)
        };
        private static string Centenas(long n)
        {
            var c = n / 100; var r = n % 100;
            var cent = c switch { 1 => "CIENTO", 5 => "QUINIENTOS", 7 => "SETECIENTOS", 9 => "NOVECIENTOS", _ => Unidades(c) + "CIENTOS" };
            return r == 0 ? cent : $"{cent} {Decenas(r)}";
        }
        private static string Miles(long n)
        {
            var m = n / 1000; var r = n % 1000;
            var mil = m == 1 ? "UN MIL" : $"{EnLetras(m)} MIL";
            return r == 0 ? mil : $"{mil} {EnLetras(r)}";
        }
        private static string Millones(long n)
        {
            var m = n / 1000000; var r = n % 1000000;
            var mill = m == 1 ? "UN MILLON" : $"{EnLetras(m)} MILLONES";
            return r == 0 ? mill : $"{mill} {EnLetras(r)}";
        }
    }
}
