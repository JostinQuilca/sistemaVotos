using System.Net;
using System.Net.Mail;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Font;           // Necesario para PdfFont
using iText.IO.Font.Constants;     // Necesario para StandardFonts

namespace SistemaVotoMVC.Helpers
{
    public class CorreoHelper
    {
        private static string _miCorreo = "sistemavotosecuador@gmail.com";
        private static string _miPassword = "yzvaucckykeaihhg";
        private static string _alias = "Sistema Electoral - CNE";

        public static async Task<(bool exitoso, string mensaje)> EnviarCertificado(string emailDestino, string nombreVotante, string cedula, long? mesaId)
        {
            try
            {
                if (string.IsNullOrEmpty(emailDestino)) return (false, "Email no válido.");

                byte[] pdfBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    PdfWriter writer = new PdfWriter(ms);
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        // Configuración de página horizontal (Landscape)
                        pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                        Document document = new Document(pdf);
                        document.SetMargins(20, 20, 20, 20);

                        // --- CARGAR FUENTES ---
                        PdfFont fontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                        PdfFont fontItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                        // --- MARCO EXTERIOR (Borde doble azul) ---
                        Table marco = new Table(1).UseAllAvailableWidth().SetHeight(500);
                        marco.SetBorder(new iText.Layout.Borders.DoubleBorder(ColorConstants.BLUE, 3));

                        Cell contenidoPrincipal = new Cell().SetPadding(30);
                        contenidoPrincipal.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                        // --- ENCABEZADO (Escudo | Título | Marca de Agua) ---
                        Table header = new Table(new float[] { 25, 50, 25 }).UseAllAvailableWidth();

                        // 1. Celda del Escudo Real
                        Cell cellEscudo = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE);
                        try
                        {
                            ImageData imgData = ImageDataFactory.Create("https://hablemosdeculturas.com/wp-content/uploads/2019/02/escudo-del-ecuador.png");
                            Image img = new Image(imgData).SetHeight(75).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                            cellEscudo.Add(img);
                        }
                        catch
                        {
                            // Si falla la descarga, la celda queda vacía (sin texto "ESCUDO")
                        }
                        header.AddCell(cellEscudo);

                        // 2. Texto Central
                        Div centro = new Div().SetTextAlignment(TextAlignment.CENTER);
                        centro.Add(new Paragraph("REPÚBLICA DEL ECUADOR").SetFont(fontBold).SetFontSize(12).SetFontColor(ColorConstants.GRAY));
                        centro.Add(new Paragraph("CERTIFICADO DE VOTACIÓN").SetFont(fontBold).SetFontSize(26).SetFontColor(ColorConstants.BLUE));
                        centro.Add(new Paragraph("Proceso Electoral 2026").SetFont(fontItalic).SetFontSize(10));
                        header.AddCell(new Cell().Add(centro).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));

                        // 3. Marca de agua "VOTÓ"
                        header.AddCell(new Cell().Add(new Paragraph("VOTÓ").SetFontSize(35).SetFontColor(ColorConstants.LIGHT_GRAY).SetOpacity(0.2f))
                            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE));

                        contenidoPrincipal.Add(header);
                        contenidoPrincipal.Add(new Paragraph("\n"));

                        // --- CUERPO ---
                        contenidoPrincipal.Add(new Paragraph("El Consejo Nacional Electoral certifica que el ciudadano(a):")
                            .SetTextAlignment(TextAlignment.CENTER).SetFontSize(14));

                        contenidoPrincipal.Add(new Paragraph(nombreVotante.ToUpper())
                            .SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER).SetFontSize(24).SetFontColor(ColorConstants.BLACK));

                        contenidoPrincipal.Add(new Paragraph("\n"));

                        // --- TABLA DE DATOS ---
                        Table datosTable = new Table(3).UseAllAvailableWidth();
                        datosTable.AddCell(CrearCeldaDato("CÉDULA DE IDENTIDAD", cedula, fontNormal, fontBold));
                        datosTable.AddCell(CrearCeldaDato("FECHA DE EMISIÓN", DateTime.Now.ToString("dd/MM/yyyy"), fontNormal, fontBold));
                        datosTable.AddCell(CrearCeldaDato("MESA ELECTORAL", "N° " + mesaId, fontNormal, fontBold));

                        contenidoPrincipal.Add(datosTable);
                        contenidoPrincipal.Add(new Paragraph("\n"));
                        contenidoPrincipal.Add(new Paragraph("Ha cumplido satisfactoriamente con su deber cívico de sufragar.")
                            .SetFont(fontItalic).SetTextAlignment(TextAlignment.CENTER).SetFontSize(12));

                        // --- SECCIÓN DE FIRMA ---
                        contenidoPrincipal.Add(new Paragraph("\n\n"));
                        Div firmaDiv = new Div().SetWidth(250).SetHorizontalAlignment(HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        firmaDiv.Add(new Paragraph("___________________________").SetFont(fontBold));
                        firmaDiv.Add(new Paragraph("PRESIDENTE DE LA JUNTA").SetFont(fontBold).SetFontSize(10));
                        firmaDiv.Add(new Paragraph("Firma Autorizada").SetFontSize(8).SetFontColor(ColorConstants.GRAY));
                        contenidoPrincipal.Add(firmaDiv);

                        marco.AddCell(contenidoPrincipal);
                        document.Add(marco);
                        document.Close();
                    }
                    pdfBytes = ms.ToArray();
                }

                // --- CONFIGURACIÓN Y ENVÍO DE GMAIL ---
                var mail = new MailMessage();
                mail.From = new MailAddress(_miCorreo, _alias);
                mail.To.Add(emailDestino);
                mail.Subject = "Certificado de Votación Oficial - CNE";
                mail.Body = $"Estimado {nombreVotante}, se adjunta su certificado oficial de votación en formato PDF.";

                using (MemoryStream attachmentStream = new MemoryStream(pdfBytes))
                {
                    mail.Attachments.Add(new Attachment(attachmentStream, $"Certificado_{cedula}.pdf", "application/pdf"));
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(_miCorreo, _miPassword);
                        await smtp.SendMailAsync(mail);
                    }
                }
                return (true, "Enviado");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        private static Cell CrearCeldaDato(string titulo, string valor, PdfFont normal, PdfFont bold)
        {
            Cell cell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER);
            cell.Add(new Paragraph(titulo).SetFont(bold).SetFontSize(8).SetFontColor(ColorConstants.GRAY));
            cell.Add(new Paragraph(valor).SetFont(bold).SetFontSize(14));
            return cell;
        }
    }
}