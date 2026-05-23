using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using static VCardix.TSModules;

namespace VCardix{
    internal class VCardixModule{
        // VCARD VERSION
        // ======================================================================================================
        public enum VCardVersion { [Description("2.1")] V21, [Description("3.0")] V30, [Description("4.0")] V40 }
        public VCardVersion CurrentVersion { get; set; } = VCardVersion.V40;
        // ID-BASED FAST ACCESS DICTIONARY
        // ======================================================================================================
        private readonly Dictionary<Guid, PrefixModule> contactsById = new Dictionary<Guid, PrefixModule>();
        // PUBLIC READONLY LIST IF YOU WANT, WE WILL PROVIDE IT, WE USED DICTIONARY FOR PERFORMANCE
        // ======================================================================================================
        public IReadOnlyCollection<PrefixModule> ContactsList => contactsById.Values;
        // PREFIX
        public class PrefixModule{
            // UID
            public Guid Id { get; set; } = Guid.NewGuid();
            // NAME
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string FullName => string.Join(" ", new[]{ FirstName, MiddleName, LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
            // BIRTHDAY
            public DateTime? Birthday { get; set; }
            // PHONE
            public string PhoneMobile { get; set; }
            public string PhoneHome { get; set; }
            public string PhoneWork { get; set; }
            // EMAIL
            public string Email1 { get; set; }
            public string Email2 { get; set; }
            public string Email3 { get; set; }
            // OTHER INFO
            public string Address { get; set; }
            public string Organization { get; set; }
            public string Website { get; set; }
            public string Note { get; set; }
            // PHOTO
            public string PhotoBase64 { get; set; }
            // IMAGE PREFIX
            public Image PhotoImage{ get { return TSImageHelper.ImageFromBase64(PhotoBase64); } }
            // IMAGE CLEAR
            public void ClearPhoto(){ PhotoBase64 = null; }

            public string CurrentDisplayMember { get; set; } = "FullName";
            public override string ToString()
            {
                // Reflection kullanarak seçili özelliği döndürüyoruz
                // Bu sayede DisplayMember ne olursa olsun ToString ona uyum sağlar
                var prop = this.GetType().GetProperty(CurrentDisplayMember);
                if (prop != null)
                {
                    return prop.GetValue(this)?.ToString() ?? "";
                }
                return FullName; // Fallback
            }
        }
        // ADD DATA
        // ======================================================================================================
        public void AddContact(PrefixModule contact){
            if (contact == null){ throw new ArgumentNullException(nameof(contact)); }
            if (contact.Id == Guid.Empty){ contact.Id = Guid.NewGuid(); }
            contactsById[contact.Id] = contact;
        }
        // UPDATE DATA
        // ======================================================================================================
        public bool UpdateContact(Guid id, PrefixModule updated){
            if (!contactsById.ContainsKey(id)){ return false; }
            var existing = contactsById[id];
            //
            existing.FirstName = updated.FirstName;
            existing.MiddleName = updated.MiddleName;
            existing.LastName = updated.LastName;
            //
            existing.Birthday = updated.Birthday;
            //
            existing.PhoneMobile = updated.PhoneMobile;
            existing.PhoneHome = updated.PhoneHome;
            existing.PhoneWork = updated.PhoneWork;
            //
            existing.Email1 = updated.Email1;
            existing.Email2 = updated.Email2;
            existing.Email3 = updated.Email3;
            //
            existing.Address = updated.Address;
            existing.Organization = updated.Organization;
            existing.Website = updated.Website;
            existing.Note = updated.Note;
            return true;
        }
        // DELETE DATA
        // ======================================================================================================
        public bool DeleteContact(Guid id){ return contactsById.Remove(id); }
        // ENCODE & DECODE QUOTED PRINTABLE
        // ======================================================================================================
        public static string EncodeQuotedPrintable(string input, int maxLineLength = 76)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(input);
            var sb = new StringBuilder();
            int linePos = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                string toAppend;
                bool isPrintable = (b >= 33 && b <= 60) || (b >= 62 && b <= 126);
                if (b == 61)
                {
                    toAppend = "=3D";
                }
                else if (isPrintable)
                {
                    toAppend = ((char)b).ToString();
                }
                else if (b == 9 || b == 32)
                {
                    bool atLineEnd = (i == bytes.Length - 1) || (linePos + 1 >= maxLineLength);
                    toAppend = atLineEnd ? "=" + b.ToString("X2") : ((char)b).ToString();
                }
                else
                {
                    toAppend = "=" + b.ToString("X2");
                }
                if (linePos + toAppend.Length > maxLineLength - 3)
                {
                    sb.Append("=\r\n");
                    linePos = 0;
                }
                sb.Append(toAppend);
                linePos += toAppend.Length;
            }
            return sb.ToString();
        }
        public static string DecodeQuotedPrintable(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            input = Regex.Replace(input, @"=\r?\n", "");
            input = Regex.Replace(input, @"=\s*$", "");
            var bytes = new List<byte>();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '=' && i + 2 < input.Length)
                {
                    string hex = input.Substring(i + 1, 2);
                    if (byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                    {
                        bytes.Add(b);
                        i += 2;
                    }
                    else
                    {
                        bytes.Add((byte)'=');
                    }
                }
                else
                {
                    bytes.Add((byte)input[i]);
                }
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        // VCARD LOAD/SAVE
        // ======================================================================================================
        public void LoadVcf(string filePath){
            contactsById.Clear();
            var rawLines = File.ReadAllLines(filePath, Encoding.UTF8);
            var lines = new List<string>();
            for (int i = 0; i < rawLines.Length; i++){
                string line = rawLines[i];
                while (line.EndsWith("=") && i + 1 < rawLines.Length){
                    line = line.Substring(0, line.Length - 1) + rawLines[i + 1];
                    i++;
                }
                lines.Add(line);
            }
            var vcardBlocks = new List<List<string>>();
            List<string> currentBlock = null;
            foreach (var line in lines){
                if (line.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)){
                    currentBlock = new List<string>();
                    vcardBlocks.Add(currentBlock);
                }
                currentBlock?.Add(line);
                if (line.StartsWith("END:VCARD", StringComparison.OrdinalIgnoreCase)){
                    currentBlock = null;
                }
            }
            string UnescapeTextV21(string text){
                if (string.IsNullOrEmpty(text))
                    return string.Empty;
                return text.Replace("\\n", "\n").Replace("\\;", ";").Replace("\\,", ",");
            }
            var lockObj = new object();
            foreach (var block in vcardBlocks){
                var current = new PrefixModule();
                string currentVersion = null;
                bool splitFn = true;
                for (int i = 0; i < block.Count; i++){
                    string line = block[i];
                    if (line.StartsWith("VERSION:", StringComparison.OrdinalIgnoreCase)){
                        currentVersion = line.Substring(8).Trim();
                    }
                    else if (line.StartsWith("UID:", StringComparison.OrdinalIgnoreCase)){
                        var uidText = line.Substring(4).Trim();
                        current.Id = Guid.TryParse(uidText, out var guid) ? guid : Guid.NewGuid();
                    }
                    else if (line.StartsWith("N:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("N;", StringComparison.OrdinalIgnoreCase)){
                        string content = line.Substring(line.IndexOf(':') + 1);
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            content = DecodeQuotedPrintable(content);
                        var parts = content.Split(';');
                        current.LastName = parts.Length > 0 ? UnescapeTextV21(parts[0]) : "";
                        current.FirstName = parts.Length > 1 ? UnescapeTextV21(parts[1]) : "";
                        current.MiddleName = parts.Length > 2 ? UnescapeTextV21(parts[2]) : "";
                    }
                    else if (line.StartsWith("FN:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("FN;", StringComparison.OrdinalIgnoreCase)){
                        string fnContent = line.Substring(line.IndexOf(':') + 1);
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            fnContent = DecodeQuotedPrintable(fnContent);
                        fnContent = UnescapeTextV21(fnContent);
                        if (splitFn){
                            var parts = fnContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0) current.FirstName = parts[0];
                            if (parts.Length > 2){
                                current.MiddleName = string.Join(" ", parts, 1, parts.Length - 2);
                                current.LastName = parts[parts.Length - 1];
                            }else if (parts.Length == 2){
                                current.LastName = parts[1];
                            }
                        }else{
                            current.FirstName = fnContent;
                            current.MiddleName = "";
                            current.LastName = "";
                        }
                    }
                    else if (line.StartsWith("BDAY:", StringComparison.OrdinalIgnoreCase)){
                        if (DateTime.TryParse(line.Substring(5).Trim(), out DateTime dt))
                            current.Birthday = dt;
                    }
                    if (line.StartsWith("TEL", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = line.IndexOf(':');
                        string header = (idx > 0 ? line.Substring(0, idx) : line).ToUpperInvariant();
                        string value = (idx > 0 ? line.Substring(idx + 1).Trim() : "");
                        value = UnescapeTextV21(value);
                        if (header.Contains("CELL") || header.Contains("MOBILE"))
                            current.PhoneMobile = value;
                        else if (header.Contains("HOME"))
                            current.PhoneHome = value;
                        else if (header.Contains("WORK"))
                            current.PhoneWork = value;
                    }
                    else if (line.StartsWith("EMAIL", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var email = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                email = DecodeQuotedPrintable(email);
                            email = UnescapeTextV21(email);
                            if (string.IsNullOrEmpty(current.Email1))
                                current.Email1 = email;
                            else if (string.IsNullOrEmpty(current.Email2))
                                current.Email2 = email;
                            else
                                current.Email3 = email;
                        }
                    }
                    else if (line.StartsWith("ADR", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var adr = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                adr = DecodeQuotedPrintable(adr);
                            adr = UnescapeTextV21(adr);
                            current.Address = adr.Replace(";", "||");
                        }
                    }
                    else if (line.StartsWith("ORG:", StringComparison.OrdinalIgnoreCase)){
                        var org = line.Substring(4).Trim();
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            org = DecodeQuotedPrintable(org);
                        current.Organization = UnescapeTextV21(org);
                    }
                    else if (line.StartsWith("URL:", StringComparison.OrdinalIgnoreCase)){
                        var url = line.Substring(4).Trim();
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            url = DecodeQuotedPrintable(url);
                        current.Website = UnescapeTextV21(url);
                    }
                    else if (line.StartsWith("NOTE:", StringComparison.OrdinalIgnoreCase)){
                        var note = line.Substring(5).Trim();
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            note = DecodeQuotedPrintable(note);
                        current.Note = UnescapeTextV21(note);
                    }
                    else if (line.StartsWith("PHOTO", StringComparison.OrdinalIgnoreCase))
                        current.PhotoBase64 = TSImageHelper.ExtractPhotoBase64(string.Join("\n", block));
                }
                if (!string.IsNullOrEmpty(currentVersion)){
                    if (currentVersion == "2.1") CurrentVersion = VCardVersion.V21;
                    else if (currentVersion == "3.0") CurrentVersion = VCardVersion.V30;
                    else if (currentVersion == "4.0") CurrentVersion = VCardVersion.V40;
                }
                lock (lockObj){
                    AddContact(current);
                }
            }
        }
        public void SaveVcf(string filePath)
        {
            var sb = new StringBuilder(ContactsList.Count * 512);
            foreach (var c in ContactsList.OrderBy(c => TSNaturalSortKey(c.FullName ?? "")))
            {
                sb.AppendLine("BEGIN:VCARD");
                switch (CurrentVersion)
                {
                    // ==============================
                    //        VCF 2.1
                    // ==============================
                    case VCardVersion.V21:
                        sb.AppendLine("VERSION:2.1");
                        sb.AppendLine($"UID:{c.Id}");
                        string EncodeQP(string text) => EncodeQuotedPrintable(text ?? "");
                        string EscapeTextV21(string text) => (text ?? "").Replace("\n", "\\n").Replace(";", "\\;").Replace(",", "\\,");
                        if (!string.IsNullOrEmpty(c.LastName) || !string.IsNullOrEmpty(c.FirstName) || !string.IsNullOrEmpty(c.MiddleName))
                        {
                            sb.AppendLine("N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:" + $"{EncodeQP(c.LastName ?? "")};" + $"{EncodeQP(c.FirstName ?? "")};" + $"{EncodeQP(c.MiddleName ?? "")};;");
                        }
                        else
                        {
                            sb.AppendLine("N:;;;;");
                        }
                        if (!string.IsNullOrEmpty(c.FullName))
                            sb.AppendLine("FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:" + EncodeQP(c.FullName));
                        if (c.Birthday.HasValue)
                            sb.AppendLine($"BDAY:{c.Birthday.Value:yyyy-MM-dd}");
                        if (!string.IsNullOrEmpty(c.PhoneMobile))
                            sb.AppendLine($"TEL;TYPE=CELL,VOICE:{EscapeTextV21(c.PhoneMobile)}");
                        if (!string.IsNullOrEmpty(c.PhoneHome))
                            sb.AppendLine($"TEL;TYPE=HOME,VOICE:{EscapeTextV21(c.PhoneHome)}");
                        if (!string.IsNullOrEmpty(c.PhoneWork))
                            sb.AppendLine($"TEL;TYPE=WORK,VOICE:{EscapeTextV21(c.PhoneWork)}");
                        if (!string.IsNullOrEmpty(c.Email1))
                            sb.AppendLine($"EMAIL;TYPE=INTERNET:{EscapeTextV21(c.Email1)}");
                        if (!string.IsNullOrEmpty(c.Email2))
                            sb.AppendLine($"EMAIL;TYPE=INTERNET:{EscapeTextV21(c.Email2)}");
                        if (!string.IsNullOrEmpty(c.Email3))
                            sb.AppendLine($"EMAIL;TYPE=INTERNET:{EscapeTextV21(c.Email3)}");
                        {
                            var parts = (c.Address?.Split(new[] { "||" }, StringSplitOptions.None) ?? new string[0]).Select(EscapeTextV21).ToList();
                            while (parts.Count < 7) parts.Add("");
                            sb.AppendLine($"ADR;TYPE=HOME:{string.Join(";", parts)}");
                        }
                        if (!string.IsNullOrEmpty(c.Organization))
                            sb.AppendLine($"ORG:{EscapeTextV21(c.Organization)}");
                        if (!string.IsNullOrEmpty(c.Website))
                            sb.AppendLine($"URL:{EscapeTextV21(c.Website)}");
                        if (!string.IsNullOrEmpty(c.Note))
                            sb.AppendLine($"NOTE:{EscapeTextV21(c.Note)}");
                        if (!string.IsNullOrEmpty(c.PhotoBase64))
                            sb.AppendLine($"PHOTO;ENCODING=BASE64;TYPE=PNG:\r\n{TSImageHelper.FoldBase64(c.PhotoBase64)}");
                        break;
                    // ==============================
                    //   VCF 3.0 & 4.0
                    // ==============================
                    default:
                        sb.AppendLine($"VERSION:{(CurrentVersion == VCardVersion.V30 ? "3.0" : "4.0")}");
                        sb.AppendLine($"UID:{c.Id}");
                        string EscapeTextV3V4(string text) => (text ?? "").Replace("\n", "\\n").Replace(";", "\\;").Replace(",", "\\,");
                        if (!string.IsNullOrEmpty(c.LastName) || !string.IsNullOrEmpty(c.FirstName) || !string.IsNullOrEmpty(c.MiddleName))
                        {
                            sb.AppendLine($"N:{EscapeTextV3V4(c.LastName)};" + $"{EscapeTextV3V4(c.FirstName)};" + $"{EscapeTextV3V4(c.MiddleName)};;");
                        }
                        else
                        {
                            sb.AppendLine("N:;;;;");
                        }
                        if (!string.IsNullOrEmpty(c.FullName))
                            sb.AppendLine($"FN:{EscapeTextV3V4(c.FullName)}");
                        if (c.Birthday.HasValue)
                            sb.AppendLine($"BDAY:{c.Birthday.Value:yyyy-MM-dd}");
                        if (!string.IsNullOrEmpty(c.PhoneMobile))
                            sb.AppendLine($"TEL;TYPE=CELL,VOICE:{EscapeTextV3V4(c.PhoneMobile)}");
                        if (!string.IsNullOrEmpty(c.PhoneHome))
                            sb.AppendLine($"TEL;TYPE=HOME,VOICE:{EscapeTextV3V4(c.PhoneHome)}");
                        if (!string.IsNullOrEmpty(c.PhoneWork))
                            sb.AppendLine($"TEL;TYPE=WORK,VOICE:{EscapeTextV3V4(c.PhoneWork)}");
                        if (!string.IsNullOrEmpty(c.Email1))
                            sb.AppendLine($"EMAIL;TYPE=INTERNET:{EscapeTextV3V4(c.Email1)}");
                        if (!string.IsNullOrEmpty(c.Email2))
                            sb.AppendLine($"EMAIL;TYPE=INTERNET:{EscapeTextV3V4(c.Email2)}");
                        if (!string.IsNullOrEmpty(c.Email3))
                            sb.AppendLine($"EMAIL;TYPE=INTERNET:{EscapeTextV3V4(c.Email3)}");
                        {
                            var parts = (c.Address?.Split(new[] { "||" }, StringSplitOptions.None) ?? new string[0]).Select(EscapeTextV3V4).ToList();
                            while (parts.Count < 7) parts.Add("");
                            sb.AppendLine($"ADR;TYPE=HOME:{string.Join(";", parts)}");
                        }
                        if (!string.IsNullOrEmpty(c.Organization))
                            sb.AppendLine($"ORG:{EscapeTextV3V4(c.Organization)}");
                        if (!string.IsNullOrEmpty(c.Website))
                            sb.AppendLine($"URL:{EscapeTextV3V4(c.Website)}");
                        if (!string.IsNullOrEmpty(c.Note))
                            sb.AppendLine($"NOTE:{EscapeTextV3V4(c.Note)}");
                        if (!string.IsNullOrEmpty(c.PhotoBase64))
                        {
                            if (CurrentVersion == VCardVersion.V30)
                            {
                                sb.AppendLine($"PHOTO;ENCODING=BASE64;TYPE=PNG:\r\n{TSImageHelper.FoldBase64(c.PhotoBase64)}");
                            }
                            else // V40
                            {
                                sb.AppendLine($"PHOTO:data:image/png;base64,{TSImageHelper.FoldBase64(c.PhotoBase64)}");
                            }
                        }
                        break;
                }
                sb.AppendLine("END:VCARD");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        // CSV LOAD/SAVE
        // ======================================================================================================
        public void LoadCsv(string filePath){
            contactsById.Clear();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0) return;
            var headers = ParseCsvLine(lines[0]);
            int FindIndex(string colName) => Array.FindIndex(headers, h => string.Equals(h, colName, StringComparison.OrdinalIgnoreCase));
            var idIndex = FindIndex("Id");
            var firstNameIndex = FindIndex("FirstName");
            var middleNameIndex = FindIndex("MiddleName");
            var lastNameIndex = FindIndex("LastName");
            var birthdayIndex = FindIndex("Birthday");
            var phoneMobileIndex = FindIndex("PhoneMobile");
            var phoneHomeIndex = FindIndex("PhoneHome");
            var phoneWorkIndex = FindIndex("PhoneWork");
            var email1Index = FindIndex("Email1");
            var email2Index = FindIndex("Email2");
            var email3Index = FindIndex("Email3");
            var addressIndex = FindIndex("Address");
            var organizationIndex = FindIndex("Organization");
            var websiteIndex = FindIndex("Website");
            var noteIndex = FindIndex("Note");
            var photoIndex = FindIndex("PhotoBase64");
            var contactList = new PrefixModule[lines.Length - 1];
            Parallel.For(1, lines.Length, i =>{
                var values = ParseCsvLine(lines[i]);
                if (values.Length == 0) return;
                var contact = new PrefixModule();
                if (idIndex >= 0 && idIndex < values.Length && Guid.TryParse(values[idIndex], out var guid))
                    contact.Id = guid;
                else
                    contact.Id = Guid.NewGuid();
                if (firstNameIndex >= 0 && firstNameIndex < values.Length)
                    contact.FirstName = values[firstNameIndex];
                if (middleNameIndex >= 0 && middleNameIndex < values.Length)
                    contact.MiddleName = values[middleNameIndex];
                if (lastNameIndex >= 0 && lastNameIndex < values.Length)
                    contact.LastName = values[lastNameIndex];
                if (birthdayIndex >= 0 && birthdayIndex < values.Length && DateTime.TryParse(values[birthdayIndex], out var dt))
                    contact.Birthday = dt;
                if (phoneMobileIndex >= 0 && phoneMobileIndex < values.Length)
                    contact.PhoneMobile = values[phoneMobileIndex];
                if (phoneHomeIndex >= 0 && phoneHomeIndex < values.Length)
                    contact.PhoneHome = values[phoneHomeIndex];
                if (phoneWorkIndex >= 0 && phoneWorkIndex < values.Length)
                    contact.PhoneWork = values[phoneWorkIndex];
                if (email1Index >= 0 && email1Index < values.Length)
                    contact.Email1 = values[email1Index];
                if (email2Index >= 0 && email2Index < values.Length)
                    contact.Email2 = values[email2Index];
                if (email3Index >= 0 && email3Index < values.Length)
                    contact.Email3 = values[email3Index];
                if (addressIndex >= 0 && addressIndex < values.Length)
                    contact.Address = values[addressIndex];
                if (organizationIndex >= 0 && organizationIndex < values.Length)
                    contact.Organization = values[organizationIndex];
                if (websiteIndex >= 0 && websiteIndex < values.Length)
                    contact.Website = values[websiteIndex];
                if (noteIndex >= 0 && noteIndex < values.Length)
                    contact.Note = values[noteIndex];
                if (photoIndex >= 0 && photoIndex < values.Length)
                    contact.PhotoBase64 = values[photoIndex];
                contactList[i - 1] = contact;
            });
            foreach (var contact in contactList){
                if (contact != null)
                    AddContact(contact);
            }
        }
        public void SaveCsv(string filePath){
            using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)){
                sw.WriteLine("Id,FirstName,MiddleName,LastName,Birthday,PhoneMobile,PhoneHome,PhoneWork,Email1,Email2,Email3,Address,Organization,Website,Note,PhotoBase64");
                foreach (var c in ContactsList.OrderBy(c => TSNaturalSortKey(c.FullName ?? ""))){
                    var line = string.Join(",",
                        EscapeCsv(c.Id.ToString()),
                        EscapeCsv(c.FirstName),
                        EscapeCsv(c.MiddleName),
                        EscapeCsv(c.LastName),
                        c.Birthday.HasValue ? c.Birthday.Value.ToString("yyyy-MM-dd") : "",
                        EscapeCsv(c.PhoneMobile),
                        EscapeCsv(c.PhoneHome),
                        EscapeCsv(c.PhoneWork),
                        EscapeCsv(c.Email1),
                        EscapeCsv(c.Email2),
                        EscapeCsv(c.Email3),
                        EscapeCsv(c.Address),
                        EscapeCsv(c.Organization),
                        EscapeCsv(c.Website),
                        EscapeCsv(c.Note),
                        EscapeCsv(c.PhotoBase64)
                    );
                    sw.WriteLine(line);
                }
            }
        }
        // ESCAPE CSV
        // ======================================================================================================
        private string EscapeCsv(string s){
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n")){
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
            return s;
        }
        // ADVANCED PARSE CVS LINE
        // ======================================================================================================
        private string[] ParseCsvLine(string line){
            var values = new List<string>();
            int i = 0;
            var sb = new StringBuilder();
            bool inQuotes = false;
            while (i < line.Length){
                char c = line[i];
                if (c == '"'){
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"'){
                        sb.Append('"');
                        i++;
                    }else{
                        inQuotes = !inQuotes;
                    }
                }else if (c == ',' && !inQuotes){
                    values.Add(sb.ToString());
                    sb.Clear();
                }else{
                    sb.Append(c);
                }
                i++;
            }
            values.Add(sb.ToString());
            return values.ToArray();
        }
        // JSON LOAD/SAVE
        // ======================================================================================================
        public void LoadJson(string filePath){
            contactsById.Clear();
            if (!File.Exists(filePath)) return;
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return;
            var serializer = new JavaScriptSerializer{ MaxJsonLength = Int32.MaxValue };
            var contacts = serializer.Deserialize<List<PrefixModule>>(json);
            if (contacts == null || contacts.Count == 0) return;
            foreach (var contact in contacts){
                if (contact.Id == Guid.Empty)
                    contact.Id = Guid.NewGuid();
                AddContact(contact);
            }
        }
        public void SaveJson(string filePath){
            var serializer = new JavaScriptSerializer{ MaxJsonLength = Int32.MaxValue };
            var orderedContacts = ContactsList.OrderBy(c => TSNaturalSortKey(c.FullName ?? "")).ToList();
            string json = serializer.Serialize(orderedContacts);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        // DYNAMIC ADVANCED SEARCH ENGINE
        // ======================================================================================================
        public IEnumerable<PrefixModule> SearchContacts(string keyword){
            if (string.IsNullOrWhiteSpace(keyword))
                return ContactsList;
            return ContactsList.AsParallel().Where(c =>
                    c.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => p.GetValue(c) as string)
                     .Any(value => !string.IsNullOrEmpty(value) && value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
        }
    }
    // TS IMAGE HELPER
    // ======================================================================================================
    public class TSImageHelper{
        // IMAGE SET AND DISPOSE - IMAGE DYNAMIC DPI & DYNAMIC RESIZER
        // =========================
        public static void SetPictureBoxImage(PictureBox pictureBox, Image newImage){
            if (pictureBox == null) return;
            Image old = pictureBox.Image;
            if (newImage == null){
                pictureBox.Image = null;
                old?.Dispose();
                return;
            }
            var resized = ResizeImageToDeviceDpi(newImage, pictureBox.Width, pictureBox.Height, pictureBox.DeviceDpi);
            pictureBox.Image = resized;
            if (old != null && !ReferenceEquals(old, newImage))
                old.Dispose();
        }
        public static Image ResizeImageToDeviceDpi(Image img, int maxWidth, int maxHeight, int deviceDpi){
            int baseW = img.Width;
            int baseH = img.Height;
            float scale = deviceDpi / 96f;
            int scaledW = (int)(baseW * scale);
            int scaledH = (int)(baseH * scale);
            double ratio = Math.Min((double)maxWidth / scaledW, (double)maxHeight / scaledH);
            int finalW = Math.Max(1, (int)(scaledW * ratio));
            int finalH = Math.Max(1, (int)(scaledH * ratio));
            var bmp = new Bitmap(finalW, finalH);
            bmp.SetResolution(deviceDpi, deviceDpi);
            using (var g = Graphics.FromImage(bmp)){
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(img, 0, 0, finalW, finalH);
            }
            return bmp;
        }
        // IMAGE TO BASE64
        // =========================
        public static Image ImageFromBase64(string base64){
            if (string.IsNullOrEmpty(base64)) return null;
            byte[] bytes = Convert.FromBase64String(base64);
            using (var ms = new MemoryStream(bytes)){
                using (var img = Image.FromStream(ms)){
                    return new Bitmap(img);
                }
            }
        }

        // BASE 64 FOLD / RFC 2426 - RFC 6350
        // =========================
        public static string FoldBase64(string base64, int foldLength = 75){
            if (string.IsNullOrEmpty(base64))
                return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < base64.Length; i += foldLength){
                int len = Math.Min(foldLength, base64.Length - i);
                if (i > 0)
                {
                    sb.Append("\r\n ");
                }
                sb.Append(base64, i, len);
            }
            return sb.ToString();
        }
        // BASE64 TO IMAGE / V2.1 - 3.0 - 4.0 | Fold & Non Fold
        // =========================
        public static string ExtractPhotoBase64(string vcf){
            string[] lines = vcf.Replace("\r", "").Split('\n');
            StringBuilder base64Builder = new StringBuilder();
            bool photoSection = false;
            Regex photoRegex = new Regex(@"PHOTO.*?base64[,:]", RegexOptions.IgnoreCase);
            foreach (string line in lines){
                if (line.StartsWith("PHOTO", StringComparison.OrdinalIgnoreCase)){
                    Match match = photoRegex.Match(line);
                    if (match.Success){
                        int idx = match.Index + match.Length;
                        base64Builder.Append(line.Substring(idx).Trim());
                        photoSection = true;
                        continue;
                    }
                    if (line.IndexOf("ENCODING=b", StringComparison.OrdinalIgnoreCase) >= 0){
                        photoSection = true;
                        continue;
                    }
                    photoSection = true;
                    continue;
                }
                if (photoSection){
                    if (line.StartsWith(" ") || line.StartsWith("\t") || !string.IsNullOrWhiteSpace(line)){
                        base64Builder.Append(line.Trim());
                    }else{
                        photoSection = false;
                    }
                }
            }
            string raw = base64Builder.ToString();
            var clean = new StringBuilder();
            foreach (char c in raw){
                if (char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=')
                    clean.Append(c);
            }
            int pad = clean.Length % 4;
            if (pad != 0)
                clean.Append(new string('=', 4 - pad));
            return clean.ToString();
        }
    }
}