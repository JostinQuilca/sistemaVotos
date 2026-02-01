using System.Net;
using System.Net.Mail;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;           // <--- NUEVO
using iText.IO.Font.Constants;     // <--- NUEVO

namespace SistemaVotoMVC.Helpers
{
    public class CorreoHelper
    {
        private static string _miCorreo = "sistemavotosecuador@gmail.com";
        private static string _miPassword = "yzva ucck ykea ihhg";
        private static string _alias = "sistemavotos";

        public static async Task<bool> EnviarCertificado(string emailDestino, string nombreVotante, string cedula, long? mesaId)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    PdfWriter writer = new PdfWriter(stream);
                    PdfDocument pdf = new PdfDocument(writer);
                    Document document = new Document(pdf);

                    // 1. CARGAR FUENTES ESTÁNDAR (Para evitar errores de SetBold)
                    PdfFont fontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    PdfFont fontItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                    // --- DISEÑO DEL PDF ---

                    // Título
                    document.Add(new Paragraph("REPÚBLICA DEL ECUADOR")
                        .SetFont(fontNormal)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(14)
                        .SetFontColor(ColorConstants.GRAY));

                    // Título Principal (Negrita y Azul)
                    Text titulo = new Text("CERTIFICADO DE VOTACIÓN").SetFont(fontBold);
                    document.Add(new Paragraph(titulo)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(24)
                        .SetFontColor(ColorConstants.BLUE));

                    // Subtítulo (Cursiva)
                    Text subtitulo = new Text("Proceso Electoral " + DateTime.Now.Year).SetFont(fontItalic);
                    document.Add(new Paragraph(subtitulo)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(10));

                    // Línea
                    document.Add(new Paragraph("___________________________________________________")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontColor(ColorConstants.LIGHT_GRAY));

                    document.Add(new Paragraph("\n"));

                    // Cuerpo
                    document.Add(new Paragraph("El Consejo Nacional Electoral certifica que el ciudadano(a):")
                        .SetFont(fontNormal)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(12));

                    // Nombre del Votante (Negrita Grande)
                    Text nombreTxt = new Text(nombreVotante.ToUpper()).SetFont(fontBold);
                    document.Add(new Paragraph(nombreTxt)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(22));

                    document.Add(new Paragraph("\n"));

                    // Tabla de Datos
                    Table table = new Table(3).UseAllAvailableWidth();

                    // Encabezados (Negrita)
                    table.AddCell(new Cell().Add(new Paragraph(new Text("CÉDULA").SetFont(fontBold)))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell().Add(new Paragraph(new Text("FECHA").SetFont(fontBold)))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell().Add(new Paragraph(new Text("MESA").SetFont(fontBold)))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

                    // Datos (Normal)
                    table.AddCell(new Cell().Add(new Paragraph(cedula).SetFont(fontNormal))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell().Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy")).SetFont(fontNormal))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell().Add(new Paragraph("N° " + (mesaId ?? 0)).SetFont(fontNormal))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

                    document.Add(table);

                    document.Add(new Paragraph("\n\n"));

                    // Despedida (Cursiva)
                    Text despedida = new Text("Ha cumplido satisfactoriamente con su deber cívico.").SetFont(fontItalic);
                    document.Add(new Paragraph(despedida)
                        .SetTextAlignment(TextAlignment.CENTER));

                    document.Add(new Paragraph("\n\n\n"));
                    document.Add(new Paragraph("__________________________")
                         .SetTextAlignment(TextAlignment.CENTER));

                    // Firma (Negrita)
                    document.Add(new Paragraph(new Text("PRESIDENTE DE LA JUNTA").SetFont(fontBold))
                         .SetTextAlignment(TextAlignment.CENTER)
                         .SetFontSize(10));

                    document.Close();

                    // 2. CONFIGURAR EL CORREO
                    var mensaje = new MailMessage();
                    mensaje.From = new MailAddress(_miCorreo, _alias);
                    mensaje.To.Add(emailDestino);
                    mensaje.Subject = "Tu Certificado de Votación (PDF)";
                    mensaje.Body = $"Estimado {nombreVotante}, adjunto encontrarás tu certificado oficial.";
                    mensaje.IsBodyHtml = false;

                    // 3. ADJUNTAR EL PDF
                    byte[] bytes = stream.ToArray();
                    mensaje.Attachments.Add(new Attachment(new MemoryStream(bytes), $"Certificado_{cedula}.pdf"));

                    // 4. ENVIAR
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(_miCorreo, _miPassword);
                        await smtp.SendMailAsync(mensaje);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error email: " + ex.Message);
                return false;
            }
        }
    }
}