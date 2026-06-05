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
        public VCardVersion CurrentVersion { get; set; } = VCardVersion.V30;
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

            // DUPLICATE CHECK: Compares by normalized name + phone fields to determine if two contacts match
            public bool IsDuplicateWith(PrefixModule other)
            {
                if (other == null) return false;

                // Normalize strings for comparison
                string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();
                string thisName = Normalize(FullName);
                string otherName = Normalize(other.FullName);
                // If both have a name, compare it
                bool nameMatches = !string.IsNullOrEmpty(thisName) && !string.IsNullOrEmpty(otherName) && thisName == otherName;
                // If names don't match, not a duplicate
                if (!string.IsNullOrEmpty(thisName) && !string.IsNullOrEmpty(otherName) && !nameMatches)
                    return false;
                // If one has no name but the other does, rely on phone comparison
                bool hasName = !string.IsNullOrEmpty(thisName) || !string.IsNullOrEmpty(otherName);
                // Collect all phone numbers for comparison
                var thisPhones = new[] { Normalize(PhoneMobile), Normalize(PhoneHome), Normalize(PhoneWork) }.Where(p => !string.IsNullOrEmpty(p)).ToHashSet();
                var otherPhones = new[] { Normalize(other.PhoneMobile), Normalize(other.PhoneHome), Normalize(other.PhoneWork) }.Where(p => !string.IsNullOrEmpty(p)).ToHashSet();
                bool phoneOverlap = thisPhones.Count > 0 && otherPhones.Count > 0 && thisPhones.Overlaps(otherPhones);
                // If names match OR phones overlap, it's a duplicate
                if (nameMatches || phoneOverlap)
                    return true;
                // If both have no name and no phone, not a duplicate (can't determine)
                if (!hasName && thisPhones.Count == 0 && otherPhones.Count == 0)
                    return false;
                return false;
            }
            public override string ToString()
            {
                var prop = this.GetType().GetProperty(CurrentDisplayMember);
                if (prop != null)
                {
                    return prop.GetValue(this)?.ToString() ?? "";
                }
                return FullName;
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
        // VCARD HELPER: RFC 6350  3.2 - Line Unfolding (remove folding CRLF followed by space/tab)
        // ======================================================================================================
        private static string UnfoldVcfLines(string raw)
        {
            // RFC 6350  3.2: Lines can be folded by inserting CRLF followed by a single space or tab.
            // Unfolding removes the CRLF and the leading space/tab.
            return Regex.Replace(raw, @"\r?\n[ \t]", "");
        }
        // VCARD HELPER: RFC 6350  3.2 - Line Folding (insert CRLF + space every 75 bytes)
        // ======================================================================================================
        private static string FoldVcfLine(string line, int maxBytes = 75)
        {
            if (string.IsNullOrEmpty(line)) return line;
            // Pre-calculate byte count for the entire remaining string
            int totalBytes = Encoding.UTF8.GetByteCount(line);
            if (totalBytes <= maxBytes)
                return line;
            // For very long strings, pre-calculate byte offsets for each character index
            // This avoids calling GetByteCount repeatedly in the inner loop
            var byteOffsets = new int[line.Length + 1];
            int runningTotal = 0;
            for (int i = 0; i < line.Length; i++)
            {
                byteOffsets[i] = runningTotal;
                runningTotal += Encoding.UTF8.GetByteCount(line[i].ToString());
            }
            byteOffsets[line.Length] = runningTotal;
            var sb = new StringBuilder();
            int idx = 0;
            while (idx < line.Length)
            {
                int remainingBytes = byteOffsets[line.Length] - byteOffsets[idx];
                if (remainingBytes <= maxBytes)
                {
                    if (sb.Length > 0)
                        sb.Append("\r\n ");
                    sb.Append(line.Substring(idx));
                    break;
                }
                else
                {
                    // Find split point using pre-calculated byte offsets
                    int splitIdx = idx + 1;
                    while (splitIdx <= line.Length && (byteOffsets[splitIdx] - byteOffsets[idx]) <= maxBytes)
                    {
                        splitIdx++;
                    }
                    // splitIdx is first index that exceeds maxBytes, so go back one
                    splitIdx--;
                    string segment = line.Substring(idx, splitIdx - idx);
                    if (sb.Length > 0)
                        sb.Append("\r\n ");
                    sb.Append(segment);
                    idx = splitIdx;
                }
            }
            return sb.ToString();
        }
        // VCARD HELPER: Unescape vCard text values per RFC 6350  3.4 /  3.3
        // ======================================================================================================
        private static string UnescapeVCardValue(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            // RFC 6350  3.4: \; => ;, \, => ,, \n or \N => newline, \\ => \
            // Also handle v2.1 \r\n-line endings
            return text.Replace("\\;", ";").Replace("\\,", ",")
                       .Replace("\\N", "\n").Replace("\\n", "\n")
                       .Replace("\\\r\n", "").Replace("\\\\", "\\");
        }
        // VCARD HELPER: Escape vCard text values per RFC 6350  3.4
        // ======================================================================================================
        private static string EscapeVCardValue(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            // Per RFC 6350  3.4: backslash, comma, semicolon, and newlines must be escaped
            return text.Replace("\\", "\\\\").Replace(";", "\\;")
                       .Replace(",", "\\,").Replace("\r\n", "\\n")
                       .Replace("\n", "\\n");
        }
        // VCARD HELPER: Escape text value per RFC 6350  3.4 for ADR components
        // (does NOT escape semicolons as they are structural separators in ADR)
        // ======================================================================================================
        private static string EscapeAdrComponent(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            // Only escape backslash, comma, and newlines - NOT semicolons (structural)
            return text.Replace("\\", "\\\\")
                       .Replace(",", "\\,")
                       .Replace("\r\n", "\\n")
                       .Replace("\n", "\\n");
        }
        // ======================================================================================================
        private static HashSet<string> GetPropertyTypes(string header, out string groupPrefix)
        {
            groupPrefix = null;
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = header.Split(';');
            var propNamePart = parts[0];
            int dotIdx = propNamePart.IndexOf('.');
            if (dotIdx >= 0)
                groupPrefix = propNamePart.Substring(0, dotIdx);
            for (int p = 1; p < parts.Length; p++)
            {
                var param = parts[p];
                // TYPE=PREF,WORK or TYPE=WORK (RFC 6350 style)
                if (param.StartsWith("TYPE=", StringComparison.OrdinalIgnoreCase))
                {
                    var valueStr = param.Substring(5);
                    foreach (var val in valueStr.Split(','))
                    {
                        var trimmed = val.Trim().ToUpperInvariant();
                        if (!string.IsNullOrEmpty(trimmed))
                            types.Add(trimmed);
                    }
                }
                // Also handle standalone type keywords (v2.1/v3.0 style): TEL;CELL;VOICE
                else if (!param.Contains("=") && !string.IsNullOrEmpty(param))
                {
                    types.Add(param.Trim().ToUpperInvariant());
                }
            }
            return types;
        }
        // VCARD LOAD/SAVE
        // ======================================================================================================
        public void LoadVcf(string filePath){
            contactsById.Clear();
            // Read raw content and unfold lines per RFC 6350  3.2
            var raw = File.ReadAllText(filePath, Encoding.UTF8);
            raw = UnfoldVcfLines(raw);
            // Split into lines after unfolding
            var allLines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var vcardBlocks = new List<List<string>>();
            List<string> currentBlock = null;
            foreach (var line in allLines){
                if (line.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)){
                    currentBlock = new List<string>();
                    vcardBlocks.Add(currentBlock);
                }
                currentBlock?.Add(line);
                if (line.StartsWith("END:VCARD", StringComparison.OrdinalIgnoreCase)){
                    currentBlock = null;
                }
            }
            var lockObj = new object();
            foreach (var block in vcardBlocks){
                var current = new PrefixModule();
                string blockVersion = null;
                for (int i = 0; i < block.Count; i++){
                    string line = block[i];
                    if (line.StartsWith("VERSION:", StringComparison.OrdinalIgnoreCase)){
                        blockVersion = line.Substring(8).Trim();
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
                        current.LastName = parts.Length > 0 ? UnescapeVCardValue(parts[0]) : "";
                        current.FirstName = parts.Length > 1 ? UnescapeVCardValue(parts[1]) : "";
                        current.MiddleName = parts.Length > 2 ? UnescapeVCardValue(parts[2]) : "";
                    }
                    else if (line.StartsWith("FN:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("FN;", StringComparison.OrdinalIgnoreCase)){
                        string fnContent = line.Substring(line.IndexOf(':') + 1);
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            fnContent = DecodeQuotedPrintable(fnContent);
                        fnContent = UnescapeVCardValue(fnContent);
                        // Only use FN to fill names if N was not provided
                        if (string.IsNullOrEmpty(current.LastName) && string.IsNullOrEmpty(current.FirstName))
                        {
                            var parts = fnContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0) current.FirstName = parts[0];
                            if (parts.Length > 2){
                                current.MiddleName = string.Join(" ", parts, 1, parts.Length - 2);
                                current.LastName = parts[parts.Length - 1];
                            }else if (parts.Length == 2){
                                current.LastName = parts[1];
                            }
                        }
                    }
                    else if (line.StartsWith("BDAY:", StringComparison.OrdinalIgnoreCase)){
                        var bdayValue = line.Substring(5).Trim();
                        // RFC 6350  6.2.5: BDAY can be DATE (yyyy-MM-dd), DATE-TIME (yyyy-MM-ddTHH:mm:ss), or TEXT
                        if (DateTime.TryParseExact(bdayValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                            current.Birthday = dt;
                        else if (DateTime.TryParseExact(bdayValue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            current.Birthday = dt;
                        else if (DateTime.TryParse(bdayValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            current.Birthday = dt;
                    }
                    if (line.StartsWith("TEL", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = line.IndexOf(':');
                        string header = (idx > 0 ? line.Substring(0, idx) : line);
                        string value = (idx > 0 ? line.Substring(idx + 1).Trim() : "");
                        value = UnescapeVCardValue(value);
                        // Strip tel: URI prefix for v4.0
                        if (value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
                            value = value.Substring(4).Trim();
                        var types = GetPropertyTypes(header, out _);
                        if (types.Contains("CELL") || types.Contains("MOBILE"))
                            current.PhoneMobile = value;
                        else if (types.Contains("HOME"))
                            current.PhoneHome = value;
                        else if (types.Contains("WORK"))
                            current.PhoneWork = value;
                    }
                    else if (line.StartsWith("EMAIL", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var email = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                email = DecodeQuotedPrintable(email);
                            email = UnescapeVCardValue(email);
                            // Check TYPE parameters for preferred/work/home email
                            int colonIdx = line.IndexOf(':');
                            string header = colonIdx > 0 ? line.Substring(0, colonIdx) : "";
                            var types = GetPropertyTypes(header, out _);
                            if (types.Contains("PREF") || types.Contains("INTERNET"))
                            {
                                // Priority: PREF marked emails go to Email1
                                if (string.IsNullOrEmpty(current.Email1))
                                    current.Email1 = email;
                                else if (string.IsNullOrEmpty(current.Email2))
                                    current.Email2 = email;
                                else
                                    current.Email3 = email;
                            }
                            else if (types.Contains("WORK"))
                            {
                                // WORK emails get priority for Email2 unless already set
                                if (string.IsNullOrEmpty(current.Email2))
                                    current.Email2 = email;
                                else if (string.IsNullOrEmpty(current.Email1))
                                    current.Email1 = email;
                                else
                                    current.Email3 = email;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(current.Email1))
                                    current.Email1 = email;
                                else if (string.IsNullOrEmpty(current.Email2))
                                    current.Email2 = email;
                                else
                                    current.Email3 = email;
                            }
                        }
                    }
                    else if (line.StartsWith("ADR", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var adr = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                adr = DecodeQuotedPrintable(adr);
                            var parts = adr.Split(';');
                            // RFC 6350  6.3.1: ADR components: PO Box, Extended, Street, Locality, Region, Postal Code, Country
                            // Store as RFC 6350 format: PO Box;Extended;Street;City;Region;Postal;Country (semicolon-separated)
                            string poBox = parts.Length > 0 ? UnescapeVCardValue(parts[0]) : "";
                            string extended = parts.Length > 1 ? UnescapeVCardValue(parts[1]) : "";
                            string street = parts.Length > 2 ? UnescapeVCardValue(parts[2]) : "";
                            string city = parts.Length > 3 ? UnescapeVCardValue(parts[3]) : "";
                            string region = parts.Length > 4 ? UnescapeVCardValue(parts[4]) : "";
                            string postal = parts.Length > 5 ? UnescapeVCardValue(parts[5]) : "";
                            string country = parts.Length > 6 ? UnescapeVCardValue(parts[6]) : "";
                            // Stored as RFC 6350 semicolon-separated format: POBox;Extended;Street;City;Region;Postal;Country
                            current.Address = string.Join(";", new[] { poBox, extended, street, city, region, postal, country });
                        }
                    }
                    else if (line.StartsWith("ORG:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("ORG;", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var org = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                org = DecodeQuotedPrintable(org);
                            // RFC 6350  6.6.4: ORG uses semicolons for hierarchy (e.g., "Company;Department")
                            var orgParts = org.Split(';');
                            current.Organization = UnescapeVCardValue(orgParts[0].Trim());
                        }
                    }
                    else if (line.StartsWith("URL:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("URL;", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var url = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                url = DecodeQuotedPrintable(url);
                            current.Website = UnescapeVCardValue(url);
                        }
                    }
                    else if (line.StartsWith("NOTE:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("NOTE;", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var note = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                note = DecodeQuotedPrintable(note);
                            current.Note = UnescapeVCardValue(note);
                        }
                    }
                    else if (line.StartsWith("PHOTO", StringComparison.OrdinalIgnoreCase))
                        current.PhotoBase64 = TSImageHelper.ExtractPhotoBase64(string.Join("\r\n", block));
                }
                // Track version per block but don't change global CurrentVersion (use last block's version)
                // The global CurrentVersion is only set by user via UI
                lock (lockObj){
                    AddContact(current);
                }
            }
        }
        // MERGE LOAD VCF: Adds contacts without clearing existing ones; skips duplicates based on name/phone
        // Returns the number of skipped (duplicate) contacts
        // ======================================================================================================
        public int MergeLoadVcf(string filePath){
            // Read raw content and unfold lines per RFC 6350  3.2
            var raw = File.ReadAllText(filePath, Encoding.UTF8);
            raw = UnfoldVcfLines(raw);
            var allLines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var vcardBlocks = new List<List<string>>();
            List<string> currentBlock = null;
            foreach (var line in allLines){
                if (line.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)){
                    currentBlock = new List<string>();
                    vcardBlocks.Add(currentBlock);
                }
                currentBlock?.Add(line);
                if (line.StartsWith("END:VCARD", StringComparison.OrdinalIgnoreCase)){
                    currentBlock = null;
                }
            }
            var lockObj = new object();
            int skippedCount = 0;
            foreach (var block in vcardBlocks){
                var current = new PrefixModule();
                string blockVersion = null;
                for (int i = 0; i < block.Count; i++){
                    string line = block[i];
                    if (line.StartsWith("VERSION:", StringComparison.OrdinalIgnoreCase)){
                        blockVersion = line.Substring(8).Trim();
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
                        current.LastName = parts.Length > 0 ? UnescapeVCardValue(parts[0]) : "";
                        current.FirstName = parts.Length > 1 ? UnescapeVCardValue(parts[1]) : "";
                        current.MiddleName = parts.Length > 2 ? UnescapeVCardValue(parts[2]) : "";
                    }
                    else if (line.StartsWith("FN:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("FN;", StringComparison.OrdinalIgnoreCase)){
                        string fnContent = line.Substring(line.IndexOf(':') + 1);
                        if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            fnContent = DecodeQuotedPrintable(fnContent);
                        fnContent = UnescapeVCardValue(fnContent);
                        if (string.IsNullOrEmpty(current.LastName) && string.IsNullOrEmpty(current.FirstName))
                        {
                            var parts = fnContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0) current.FirstName = parts[0];
                            if (parts.Length > 2){
                                current.MiddleName = string.Join(" ", parts, 1, parts.Length - 2);
                                current.LastName = parts[parts.Length - 1];
                            }else if (parts.Length == 2){
                                current.LastName = parts[1];
                            }
                        }
                    }
                    else if (line.StartsWith("BDAY:", StringComparison.OrdinalIgnoreCase)){
                        var bdayValue = line.Substring(5).Trim();
                        if (DateTime.TryParseExact(bdayValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                            current.Birthday = dt;
                        else if (DateTime.TryParseExact(bdayValue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            current.Birthday = dt;
                        else if (DateTime.TryParse(bdayValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            current.Birthday = dt;
                    }
                    if (line.StartsWith("TEL", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = line.IndexOf(':');
                        string header = (idx > 0 ? line.Substring(0, idx) : line);
                        string value = (idx > 0 ? line.Substring(idx + 1).Trim() : "");
                        value = UnescapeVCardValue(value);
                        if (value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
                            value = value.Substring(4).Trim();
                        var types = GetPropertyTypes(header, out _);
                        if (types.Contains("CELL") || types.Contains("MOBILE"))
                            current.PhoneMobile = value;
                        else if (types.Contains("HOME"))
                            current.PhoneHome = value;
                        else if (types.Contains("WORK"))
                            current.PhoneWork = value;
                    }
                    else if (line.StartsWith("EMAIL", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var email = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                email = DecodeQuotedPrintable(email);
                            email = UnescapeVCardValue(email);
                            int colonIdx = line.IndexOf(':');
                            string header = colonIdx > 0 ? line.Substring(0, colonIdx) : "";
                            var types = GetPropertyTypes(header, out _);
                            if (types.Contains("PREF") || types.Contains("INTERNET"))
                            {
                                if (string.IsNullOrEmpty(current.Email1))
                                    current.Email1 = email;
                                else if (string.IsNullOrEmpty(current.Email2))
                                    current.Email2 = email;
                                else
                                    current.Email3 = email;
                            }
                            else if (types.Contains("WORK"))
                            {
                                if (string.IsNullOrEmpty(current.Email2))
                                    current.Email2 = email;
                                else if (string.IsNullOrEmpty(current.Email1))
                                    current.Email1 = email;
                                else
                                    current.Email3 = email;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(current.Email1))
                                    current.Email1 = email;
                                else if (string.IsNullOrEmpty(current.Email2))
                                    current.Email2 = email;
                                else
                                    current.Email3 = email;
                            }
                        }
                    }
                    else if (line.StartsWith("ADR", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var adr = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                adr = DecodeQuotedPrintable(adr);
                            var parts = adr.Split(';');
                            string poBox = parts.Length > 0 ? UnescapeVCardValue(parts[0]) : "";
                            string extended = parts.Length > 1 ? UnescapeVCardValue(parts[1]) : "";
                            string street = parts.Length > 2 ? UnescapeVCardValue(parts[2]) : "";
                            string city = parts.Length > 3 ? UnescapeVCardValue(parts[3]) : "";
                            string region = parts.Length > 4 ? UnescapeVCardValue(parts[4]) : "";
                            string postal = parts.Length > 5 ? UnescapeVCardValue(parts[5]) : "";
                            string country = parts.Length > 6 ? UnescapeVCardValue(parts[6]) : "";
                            current.Address = string.Join(";", new[] { poBox, extended, street, city, region, postal, country });
                        }
                    }
                    else if (line.StartsWith("ORG:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("ORG;", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var org = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                org = DecodeQuotedPrintable(org);
                            current.Organization = UnescapeVCardValue(org);
                        }
                    }
                    else if (line.StartsWith("URL:", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0)
                            current.Website = line.Substring(idx + 1).Trim();
                    }
                    else if (line.StartsWith("NOTE:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("NOTE;", StringComparison.OrdinalIgnoreCase)){
                        var idx = line.IndexOf(':');
                        if (idx >= 0){
                            var note = line.Substring(idx + 1).Trim();
                            if (line.IndexOf("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                                note = DecodeQuotedPrintable(note);
                            current.Note = UnescapeVCardValue(note);
                        }
                    }
                    else if (line.StartsWith("PHOTO", StringComparison.OrdinalIgnoreCase)){
                        current.PhotoBase64 = TSImageHelper.ExtractPhotoBase64(string.Join("\r\n", block));
                    }
                }
                lock (lockObj){
                    // Check for duplicates before adding
                    bool isDuplicate = contactsById.Values.Any(existing => existing.IsDuplicateWith(current));
                    if (!isDuplicate){
                        current.Id = Guid.NewGuid();
                        AddContact(current);
                    }else{
                        skippedCount++;
                    }
                }
            }
            return skippedCount;
        }
        // VCARD HELPER: Normalize phone number to E.164 format for v4.0 tel: URI
        // ======================================================================================================
        private static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return phone;
            // Remove all non-digit characters except leading +
            var digitsOnly = Regex.Replace(phone, @"[^\d\+]", "");
            // If it already starts with +, treat as E.164
            if (digitsOnly.StartsWith("+"))
                return digitsOnly;
            // If it starts with 00, convert to +
            if (digitsOnly.StartsWith("00"))
                return "+" + digitsOnly.Substring(2);
            return digitsOnly;
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
                    // VCF 2.1
                    // ==============================
                    case VCardVersion.V21:
                        sb.AppendLine("VERSION:2.1");
                        sb.AppendLine($"UID:{c.Id}");
                        string EncodeQP(string text) => EncodeQuotedPrintable(text ?? "");
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
                            sb.AppendLine($"TEL;CELL;VOICE:{c.PhoneMobile}");
                        if (!string.IsNullOrEmpty(c.PhoneHome))
                            sb.AppendLine($"TEL;HOME;VOICE:{c.PhoneHome}");
                        if (!string.IsNullOrEmpty(c.PhoneWork))
                            sb.AppendLine($"TEL;WORK;VOICE:{c.PhoneWork}");
                        if (!string.IsNullOrEmpty(c.Email1))
                            sb.AppendLine($"EMAIL;INTERNET:{EscapeVCardValue(c.Email1)}");
                        if (!string.IsNullOrEmpty(c.Email2))
                            sb.AppendLine($"EMAIL;INTERNET:{EscapeVCardValue(c.Email2)}");
                        if (!string.IsNullOrEmpty(c.Email3))
                            sb.AppendLine($"EMAIL;INTERNET:{EscapeVCardValue(c.Email3)}");
                                                {
                            var addressComps = (c.Address ?? "").Split(new[] { ';' }, StringSplitOptions.None);
                            // RFC 6350  6.3.1: ADR = PO Box;Extended;Street;Locality;Region;Postal;Country
                            var parts = new List<string>
                            {
                                addressComps.Length > 0 ? EscapeAdrComponent(addressComps[0]) : "",
                                addressComps.Length > 1 ? EscapeAdrComponent(addressComps[1]) : "",
                                addressComps.Length > 2 ? EscapeAdrComponent(addressComps[2]) : "",
                                addressComps.Length > 3 ? EscapeAdrComponent(addressComps[3]) : "",
                                addressComps.Length > 4 ? EscapeAdrComponent(addressComps[4]) : "",
                                addressComps.Length > 5 ? EscapeAdrComponent(addressComps[5]) : "",
                                addressComps.Length > 6 ? EscapeAdrComponent(addressComps[6]) : ""
                            };
                            if (parts.Any(p => !string.IsNullOrEmpty(p)))
                                sb.AppendLine($"ADR;HOME:{string.Join(";", parts)}");
                        }
                        if (!string.IsNullOrEmpty(c.Organization))
                            sb.AppendLine($"ORG:{EscapeVCardValue(c.Organization)}");
                        if (!string.IsNullOrEmpty(c.Website))
                            sb.AppendLine($"URL:{EscapeVCardValue(c.Website)}");
                        if (!string.IsNullOrEmpty(c.Note))
                            sb.AppendLine($"NOTE:{EscapeVCardValue(c.Note)}");
                        if (!string.IsNullOrEmpty(c.PhotoBase64))
                            sb.AppendLine($"PHOTO;ENCODING=BASE64;TYPE={TSImageHelper.DetectMimeTypeFromBase64(c.PhotoBase64)}:\r\n{TSImageHelper.FoldBase64(c.PhotoBase64)}");
                        break;
                    // ==============================
                    // VCF 3.0
                    // ==============================
                    case VCardVersion.V30:
                        sb.AppendLine("VERSION:3.0");
                        sb.AppendLine($"PRODID:-//{Application.ProductName}//{TS_VersionEngine.TS_SoftwareVersion(1)}//EN");
                        sb.AppendLine($"UID:{c.Id}");
                        if (!string.IsNullOrEmpty(c.LastName) || !string.IsNullOrEmpty(c.FirstName) || !string.IsNullOrEmpty(c.MiddleName))
                        {
                            sb.AppendLine(FoldVcfLine($"N:{EscapeVCardValue(c.LastName)};{EscapeVCardValue(c.FirstName)};{EscapeVCardValue(c.MiddleName)};;"));
                        }
                        else
                        {
                            sb.AppendLine("N:;;;;");
                        }
                        if (!string.IsNullOrEmpty(c.FullName))
                            sb.AppendLine(FoldVcfLine($"FN:{EscapeVCardValue(c.FullName)}"));
                        if (c.Birthday.HasValue)
                            sb.AppendLine($"BDAY:{c.Birthday.Value:yyyy-MM-dd}");
                                                if (!string.IsNullOrEmpty(c.PhoneMobile))
                            sb.AppendLine(FoldVcfLine($"TEL;TYPE=CELL,VOICE:{c.PhoneMobile}"));
                        if (!string.IsNullOrEmpty(c.PhoneHome))
                            sb.AppendLine(FoldVcfLine($"TEL;TYPE=HOME,VOICE:{c.PhoneHome}"));
                        if (!string.IsNullOrEmpty(c.PhoneWork))
                            sb.AppendLine(FoldVcfLine($"TEL;TYPE=WORK,VOICE:{c.PhoneWork}"));
                        if (!string.IsNullOrEmpty(c.Email1))
                            sb.AppendLine(FoldVcfLine($"EMAIL;TYPE=INTERNET:{EscapeVCardValue(c.Email1)}"));
                        if (!string.IsNullOrEmpty(c.Email2))
                            sb.AppendLine(FoldVcfLine($"EMAIL;TYPE=INTERNET:{EscapeVCardValue(c.Email2)}"));
                        if (!string.IsNullOrEmpty(c.Email3))
                            sb.AppendLine(FoldVcfLine($"EMAIL;TYPE=INTERNET:{EscapeVCardValue(c.Email3)}"));
                        {
                            var addressComps = (c.Address ?? "").Split(new[] { ';' }, StringSplitOptions.None);
                            var parts = new List<string>
                            {
                                addressComps.Length > 0 ? EscapeAdrComponent(addressComps[0]) : "",
                                addressComps.Length > 1 ? EscapeAdrComponent(addressComps[1]) : "",
                                addressComps.Length > 2 ? EscapeAdrComponent(addressComps[2]) : "",
                                addressComps.Length > 3 ? EscapeAdrComponent(addressComps[3]) : "",
                                addressComps.Length > 4 ? EscapeAdrComponent(addressComps[4]) : "",
                                addressComps.Length > 5 ? EscapeAdrComponent(addressComps[5]) : "",
                                addressComps.Length > 6 ? EscapeAdrComponent(addressComps[6]) : ""
                            };
                            if (parts.Any(p => !string.IsNullOrEmpty(p)))
                                sb.AppendLine(FoldVcfLine($"ADR;TYPE=HOME:{string.Join(";", parts)}"));
                        }
                        if (!string.IsNullOrEmpty(c.Organization))
                            sb.AppendLine(FoldVcfLine($"ORG:{EscapeVCardValue(c.Organization)}"));
                        if (!string.IsNullOrEmpty(c.Website))
                            sb.AppendLine(FoldVcfLine($"URL:{EscapeVCardValue(c.Website)}"));
                        if (!string.IsNullOrEmpty(c.Note))
                            sb.AppendLine(FoldVcfLine($"NOTE:{EscapeVCardValue(c.Note)}"));
                        if (!string.IsNullOrEmpty(c.PhotoBase64))
                            sb.AppendLine($"PHOTO;ENCODING=BASE64;TYPE={TSImageHelper.DetectMimeTypeFromBase64(c.PhotoBase64)}:\r\n{TSImageHelper.FoldBase64(c.PhotoBase64)}");
                        break;
                    // ==============================
                    // VCF 4.0
                    // ==============================
                    default:
                        sb.AppendLine("VERSION:4.0");
                        sb.AppendLine($"PRODID:-//{Application.ProductName}//{TS_VersionEngine.TS_SoftwareVersion(1)}//EN");
                        sb.AppendLine($"UID:{c.Id}");
                        if (!string.IsNullOrEmpty(c.LastName) || !string.IsNullOrEmpty(c.FirstName) || !string.IsNullOrEmpty(c.MiddleName))
                        {
                            sb.AppendLine(FoldVcfLine($"N:{EscapeVCardValue(c.LastName)};{EscapeVCardValue(c.FirstName)};{EscapeVCardValue(c.MiddleName)};;"));
                        }
                        else
                        {
                            sb.AppendLine("N:;;;;");
                        }
                        if (!string.IsNullOrEmpty(c.FullName))
                            sb.AppendLine(FoldVcfLine($"FN:{EscapeVCardValue(c.FullName)}"));
                        if (c.Birthday.HasValue)
                            sb.AppendLine($"BDAY:{c.Birthday.Value:yyyy-MM-dd}");
                        // RFC 6350  6.4.1: TEL in v4.0 must use tel: URI format
                        if (!string.IsNullOrEmpty(c.PhoneMobile))
                            sb.AppendLine(FoldVcfLine($"TEL;TYPE=cell,voice:tel:{NormalizePhoneNumber(c.PhoneMobile)}"));
                        if (!string.IsNullOrEmpty(c.PhoneHome))
                            sb.AppendLine(FoldVcfLine($"TEL;TYPE=home,voice:tel:{NormalizePhoneNumber(c.PhoneHome)}"));
                        if (!string.IsNullOrEmpty(c.PhoneWork))
                            sb.AppendLine(FoldVcfLine($"TEL;TYPE=work,voice:tel:{NormalizePhoneNumber(c.PhoneWork)}"));
                        // RFC 6350: EMAIL uses TYPE parameter
                        if (!string.IsNullOrEmpty(c.Email1))
                            sb.AppendLine(FoldVcfLine($"EMAIL:{EscapeVCardValue(c.Email1)}"));
                        if (!string.IsNullOrEmpty(c.Email2))
                            sb.AppendLine(FoldVcfLine($"EMAIL:{EscapeVCardValue(c.Email2)}"));
                        if (!string.IsNullOrEmpty(c.Email3))
                            sb.AppendLine(FoldVcfLine($"EMAIL:{EscapeVCardValue(c.Email3)}"));
                        {
                            var addressComps = (c.Address ?? "").Split(new[] { ';' }, StringSplitOptions.None);
                            var parts = new List<string>
                            {
                                addressComps.Length > 0 ? EscapeAdrComponent(addressComps[0]) : "",
                                addressComps.Length > 1 ? EscapeAdrComponent(addressComps[1]) : "",
                                addressComps.Length > 2 ? EscapeAdrComponent(addressComps[2]) : "",
                                addressComps.Length > 3 ? EscapeAdrComponent(addressComps[3]) : "",
                                addressComps.Length > 4 ? EscapeAdrComponent(addressComps[4]) : "",
                                addressComps.Length > 5 ? EscapeAdrComponent(addressComps[5]) : "",
                                addressComps.Length > 6 ? EscapeAdrComponent(addressComps[6]) : ""
                            };
                            if (parts.Any(p => !string.IsNullOrEmpty(p)))
                                sb.AppendLine(FoldVcfLine($"ADR;TYPE=home:{string.Join(";", parts)}"));
                        }
                        if (!string.IsNullOrEmpty(c.Organization))
                            sb.AppendLine(FoldVcfLine($"ORG:{EscapeVCardValue(c.Organization)}"));
                        if (!string.IsNullOrEmpty(c.Website))
                            sb.AppendLine(FoldVcfLine($"URL:{EscapeVCardValue(c.Website)}"));
                        if (!string.IsNullOrEmpty(c.Note))
                            sb.AppendLine(FoldVcfLine($"NOTE:{EscapeVCardValue(c.Note)}"));
                        if (!string.IsNullOrEmpty(c.PhotoBase64))
                        {
                            // RFC 6350  6.2.4: PHOTO uses URI format (data URI)
                            // Use FoldBase64 for performance (same as v2.1/v3.0) instead of FoldVcfLine
                            sb.AppendLine($"PHOTO:data:{TSImageHelper.DetectMimeTypeFromBase64(c.PhotoBase64)};base64,\r\n{TSImageHelper.FoldBase64(c.PhotoBase64)}");
                        }
                        break;
                }
                sb.AppendLine("END:VCARD");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        // CSV LOAD/SAVE
        // ======================================================================================================
        public void LoadCsv(string filePath)
        {
            contactsById.Clear();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0) return;
            var headers = ParseCsvLine(lines[0]);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i]?.Trim();
                if (string.IsNullOrEmpty(h)) continue;
                string lower = h.ToLowerInvariant();
                if (lower == "first name" || lower == "given name")
                    map[h] = "FirstName";
                else if (lower == "middle name" || lower == "additional name")
                    map[h] = "MiddleName";
                else if (lower == "last name" || lower == "family name" || lower == "surname")
                    map[h] = "LastName";
                else if (lower == "birthday" || lower == "birth date" || lower == "bday")
                    map[h] = "Birthday";
                else if (lower == "organization name" || lower == "org" || lower == "company")
                    map[h] = "Organization";
                else if (lower == "notes" || lower == "note")
                    map[h] = "Note";
                else if (lower == "website" || lower == "url" || lower == "website 1 - value")
                    map[h] = "Website";
                else if (lower.StartsWith("address") && (lower.Contains("street") || lower.Contains("city") || lower.Contains("postal") || lower.Contains("region") || lower.Contains("country")))
                    map[h] = "Address";
                else if ((lower.Contains("phone") && lower.Contains("type")) || (lower.Contains("phone") && lower.Contains("label")))
                    map[h] = "PhoneType";
                else if (lower.Contains("phone") && lower.Contains("value"))
                    map[h] = "PhoneValue";
                else if (lower.StartsWith("e-mail") && lower.Contains("type"))
                    map[h] = "EmailType";
                else if (lower.StartsWith("e-mail") && lower.Contains("value"))
                    map[h] = "EmailValue";
                else if (lower == "photo" || lower == "photobase64")
                    map[h] = "PhotoBase64";
                else if (lower.StartsWith("address") && lower.Contains("type"))
                    map[h] = "AddressType";
                else
                    map[h] = h;
            }
            var contactList = new PrefixModule[lines.Length - 1];
            Parallel.For(1, lines.Length, i =>
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Length == 0) return;
                var contact = new PrefixModule { Id = Guid.NewGuid() };
                var phones = new List<(string type, string value)>();
                var emails = new List<(string type, string value)>();
                string addressStreet = null, addressCity = null, addressPostal = null, addressRegion = null, addressCountry = null;
                string addressType = null;
                for (int col = 0; col < headers.Length && col < values.Length; col++)
                {
                    string header = headers[col];
                    string val = values[col]?.Trim();
                    if (string.IsNullOrEmpty(val)) continue;
                    if (!map.TryGetValue(header, out string target)) continue;
                    switch (target)
                    {
                        case "FirstName": contact.FirstName = val; break;
                        case "MiddleName": contact.MiddleName = val; break;
                        case "LastName": contact.LastName = val; break;
                        case "Birthday":
                            if (DateTime.TryParse(val, out var dt)) contact.Birthday = dt;
                            break;
                        case "Organization": contact.Organization = val; break;
                        case "Note": contact.Note = val; break;
                        case "Website": contact.Website = val; break;
                        case "PhotoBase64": contact.PhotoBase64 = val; break;
                        case "PhoneType":
                            if (col + 1 < values.Length && map.TryGetValue(headers[col + 1], out string nextTarget) && nextTarget == "PhoneValue")
                            {
                                string phoneVal = values[col + 1]?.Trim();
                                if (!string.IsNullOrEmpty(phoneVal))
                                    phones.Add((val, phoneVal));
                            }
                            break;
                        case "EmailType":
                            if (col + 1 < values.Length && map.TryGetValue(headers[col + 1], out string nextEmailTarget) && nextEmailTarget == "EmailValue")
                            {
                                string emailVal = values[col + 1]?.Trim();
                                if (!string.IsNullOrEmpty(emailVal))
                                    emails.Add((val, emailVal));
                            }
                            break;
                        case "AddressType":
                            addressType = val;
                            break;
                        case "Address":
                            string lowerHeader = header.ToLowerInvariant();
                            if (lowerHeader.Contains("street") || lowerHeader.Contains("address line"))
                                addressStreet = val;
                            else if (lowerHeader.Contains("city"))
                                addressCity = val;
                            else if (lowerHeader.Contains("postal") || lowerHeader.Contains("zip"))
                                addressPostal = val;
                            else if (lowerHeader.Contains("region") || lowerHeader.Contains("state"))
                                addressRegion = val;
                            else if (lowerHeader.Contains("country"))
                                addressCountry = val;
                            break;
                    }
                }
                foreach (var (type, value) in phones)
                {
                    string lowerType = type.ToLowerInvariant();
                    if (lowerType.Contains("mobile") || lowerType.Contains("cell") || lowerType.Contains("cep") || lowerType.Contains("mobil") || lowerType.Contains("cellular"))
                        contact.PhoneMobile = value;
                    else if (lowerType.Contains("home") || lowerType.Contains("ev"))
                        contact.PhoneHome = value;
                    else if (lowerType.Contains("work") || lowerType.Contains("iş") || lowerType.Contains("business") || lowerType.Contains("office") || lowerType.Contains("company"))
                        contact.PhoneWork = value;
                    else if (string.IsNullOrEmpty(contact.PhoneMobile))
                        contact.PhoneMobile = value;
                }
                int emailIdx = 0;
                foreach (var (type, value) in emails)
                {
                    if (emailIdx == 0) contact.Email1 = value;
                    else if (emailIdx == 1) contact.Email2 = value;
                    else if (emailIdx == 2) contact.Email3 = value;
                    emailIdx++;
                    if (emailIdx >= 3) break;
                }
                if (!string.IsNullOrEmpty(addressStreet) || !string.IsNullOrEmpty(addressCity))
                {
                    string[] adrParts = new string[7];
                    adrParts[2] = addressStreet ?? "";
                    adrParts[3] = addressCity ?? "";
                    adrParts[4] = addressRegion ?? "";
                    adrParts[5] = addressPostal ?? "";
                    adrParts[6] = addressCountry ?? "";
                    contact.Address = string.Join(";", adrParts);
                }
                contactList[i - 1] = contact;
            });
            foreach (var contact in contactList)
            {
                if (contact != null) AddContact(contact);
            }
        }
        // MERGE LOAD CSV: Adds contacts without clearing existing ones; skips duplicates based on name/phone
        // Returns the number of skipped (duplicate) contacts
        // ======================================================================================================
        public int MergeLoadCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0) return 0;
            var headers = ParseCsvLine(lines[0]);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i]?.Trim();
                if (string.IsNullOrEmpty(h)) continue;
                string lower = h.ToLowerInvariant();
                if (lower == "first name" || lower == "given name")
                    map[h] = "FirstName";
                else if (lower == "middle name" || lower == "additional name")
                    map[h] = "MiddleName";
                else if (lower == "last name" || lower == "family name" || lower == "surname")
                    map[h] = "LastName";
                else if (lower == "birthday" || lower == "birth date" || lower == "bday")
                    map[h] = "Birthday";
                else if (lower == "organization name" || lower == "org" || lower == "company")
                    map[h] = "Organization";
                else if (lower == "notes" || lower == "note")
                    map[h] = "Note";
                else if (lower == "website 1 - value" || lower == "website" || lower == "url")
                    map[h] = "Website";
                else if (lower.Contains("phone") && lower.Contains("mobile") || lower.Contains("phone") && lower.Contains("cell"))
                    map[h] = "PhoneMobile";
                else if (lower.Contains("phone") && lower.Contains("home"))
                    map[h] = "PhoneHome";
                else if (lower.Contains("phone") && lower.Contains("work"))
                    map[h] = "PhoneWork";
                else if ((lower.Contains("email") || lower.Contains("e-mail")) && lower.Contains("1"))
                    map[h] = "Email1";
                else if ((lower.Contains("email") || lower.Contains("e-mail")) && lower.Contains("2"))
                    map[h] = "Email2";
                else if ((lower.Contains("email") || lower.Contains("e-mail")) && lower.Contains("3"))
                    map[h] = "Email3";
                else if (lower.Contains("address") || lower.Contains("street"))
                    map[h] = "Address";
                else if (lower.Contains("photo") || lower.Contains("image") || lower.Contains("base64"))
                    map[h] = "PhotoBase64";
                else
                    map[h] = h;
            }
            var contactList = new PrefixModule[lines.Length - 1];
            int skippedCount = 0;
            Parallel.For(1, lines.Length, i =>
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Length == 0) return;
                var contact = new PrefixModule { Id = Guid.NewGuid() };
                var phones = new List<(string type, string value)>();
                var emails = new List<(string type, string value)>();
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    string header = headers[j]?.Trim();
                    string value = values[j]?.Trim();
                    if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(value)) continue;
                    if (!map.TryGetValue(header, out string mapped)) continue;
                    switch (mapped)
                    {
                        case "FirstName": contact.FirstName = value; break;
                        case "MiddleName": contact.MiddleName = value; break;
                        case "LastName": contact.LastName = value; break;
                        case "Birthday":
                            if (DateTime.TryParse(value, out DateTime bd)) contact.Birthday = bd;
                            break;
                        case "Organization": contact.Organization = value; break;
                        case "Note": contact.Note = value; break;
                        case "Website": contact.Website = value; break;
                        case "PhoneMobile": contact.PhoneMobile = value; break;
                        case "PhoneHome": contact.PhoneHome = value; break;
                        case "PhoneWork": contact.PhoneWork = value; break;
                        case "Email1": contact.Email1 = value; break;
                        case "Email2": contact.Email2 = value; break;
                        case "Email3": contact.Email3 = value; break;
                        case "Address": contact.Address = value; break;
                        case "PhotoBase64": contact.PhotoBase64 = value; break;
                    }
                }
                contactList[i - 1] = contact;
            });
            foreach (var contact in contactList)
            {
                if (contact != null)
                {
                    // Check for duplicates before adding
                    lock (contactsById)
                    {
                        bool isDuplicate = contactsById.Values.Any(existing => existing.IsDuplicateWith(contact));
                        if (!isDuplicate)
                        {
                            contact.Id = Guid.NewGuid();
                            contactsById[contact.Id] = contact;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
            }
            return skippedCount;
        }
        public void SaveCsv(string filePath, bool includePhoto = false)
        {
            using (var sw = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                string header = "\"First Name\",\"Middle Name\",\"Last Name\",\"Organization Name\",\"Birthday\",\"Notes\",\"Website 1 - Value\"," +
                                "\"Phone 1 - Type\",\"Phone 1 - Value\",\"Phone 2 - Type\",\"Phone 2 - Value\",\"Phone 3 - Type\",\"Phone 3 - Value\"," +
                                "\"E-mail 1 - Type\",\"E-mail 1 - Value\",\"E-mail 2 - Type\",\"E-mail 2 - Value\",\"E-mail 3 - Type\",\"E-mail 3 - Value\"," +
                                "\"Address 1 - Type\",\"Address 1 - Street\",\"Address 1 - City\",\"Address 1 - Postal Code\",\"Address 1 - Region\",\"Address 1 - Country\"";
                if (includePhoto)
                {
                    header += ",\"PhotoBase64\"";
                }
                sw.WriteLine(header);
                foreach (var c in ContactsList.OrderBy(c => TSNaturalSortKey(c.FullName ?? "")))
                {
                    var addressParts = (c.Address ?? "").Split(new[] { ';' }, StringSplitOptions.None);
                    string street = addressParts.Length > 2 ? addressParts[2] : "";
                    string city = addressParts.Length > 3 ? addressParts[3] : "";
                    string postal = addressParts.Length > 5 ? addressParts[5] : "";
                    string region = addressParts.Length > 4 ? addressParts[4] : "";
                    string country = addressParts.Length > 6 ? addressParts[6] : "";
                    var phoneTypes = new[] { "Mobile", "Home", "Work" };
                    var phoneValues = new[] { c.PhoneMobile, c.PhoneHome, c.PhoneWork };
                    var emailTypes = new[] { "Work", "Other", "Other" };
                    var emailValues = new[] { c.Email1, c.Email2, c.Email3 };
                    string addressType = "Home";
                    var row = new List<string>
                    {
                        EscapeCsv(c.FirstName), EscapeCsv(c.MiddleName), EscapeCsv(c.LastName),
                        EscapeCsv(c.Organization),
                        c.Birthday?.ToString("yyyy-MM-dd") ?? "",
                        EscapeCsv(c.Note),
                        EscapeCsv(c.Website),
                        // Phone 1
                        EscapeCsv(phoneTypes[0]), EscapeCsv(phoneValues[0]),
                        // Phone 2
                        EscapeCsv(phoneTypes[1]), EscapeCsv(phoneValues[1]),
                        // Phone 3
                        EscapeCsv(phoneTypes[2]), EscapeCsv(phoneValues[2]),
                        // E-Mail 1
                        EscapeCsv(emailTypes[0]), EscapeCsv(emailValues[0]),
                        // E-Mail 2
                        EscapeCsv(emailTypes[1]), EscapeCsv(emailValues[1]),
                        // E-Mail 3
                        EscapeCsv(emailTypes[2]), EscapeCsv(emailValues[2]),
                        // Adress
                        EscapeCsv(addressType), EscapeCsv(street), EscapeCsv(city), EscapeCsv(postal), EscapeCsv(region), EscapeCsv(country)
                    };
                    if (includePhoto)
                    {
                        row.Add(EscapeCsv(c.PhotoBase64));
                    }
                    sw.WriteLine(string.Join(",", row));
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
        // MERGE LOAD JSON: Adds contacts without clearing existing ones; skips duplicates based on name/phone
        // Returns the number of skipped (duplicate) contacts
        // ======================================================================================================
        public int MergeLoadJson(string filePath){
            if (!File.Exists(filePath)) return 0;
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return 0;
            var serializer = new JavaScriptSerializer{ MaxJsonLength = Int32.MaxValue };
            var contacts = serializer.Deserialize<List<PrefixModule>>(json);
            if (contacts == null || contacts.Count == 0) return 0;
            int skippedCount = 0;
            foreach (var contact in contacts){
                // Check for duplicates before adding
                bool isDuplicate = contactsById.Values.Any(existing => existing.IsDuplicateWith(contact));
                if (!isDuplicate)
                {
                    if (contact.Id == Guid.Empty)
                        contact.Id = Guid.NewGuid();
                    contactsById[contact.Id] = contact;
                }
                else
                {
                    skippedCount++;
                }
            }
            return skippedCount;
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
        // DETECT MIME TYPE FROM BASE64 DATA (PNG, JPEG, GIF, BMP, TIFF)
        // =========================
        public static string DetectMimeTypeFromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return "image/png";
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                if (bytes.Length < 4) return "image/png";
                // Check magic bytes
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                    return "image/png";
                if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                    return "image/jpeg";
                if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                    return "image/gif";
                if (bytes[0] == 0x42 && bytes[1] == 0x4D)
                    return "image/bmp";
                if ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
                    (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A))
                    return "image/tiff";
                if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x01 && bytes[3] == 0x00)
                    return "image/x-icon";
                return "image/png";
            }
            catch
            {
                return "image/png";
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
            // The vcf content is already unfolded when this is called from LoadVcf,
            // but we still handle folded content for backward compatibility.
            // First unfold the PHOTO section if needed
            string processed = Regex.Replace(vcf, @"\r?\n[ \t]", "");
            string[] lines = processed.Replace("\r", "").Split('\n');
            StringBuilder base64Builder = new StringBuilder();
            bool photoSection = false;
            // v4.0: PHOTO:data:image/png;base64,iVBOR...
            Regex dataUriRegex = new Regex(@"base64[,:]", RegexOptions.IgnoreCase);
            foreach (string line in lines)
            {
                if (line.StartsWith("PHOTO", StringComparison.OrdinalIgnoreCase))
                {
                    Match match = dataUriRegex.Match(line);
                    if (match.Success)
                    {
                        // v4.0 data URI: base64, veya base64: sonrası base64
                        int idx = match.Index + match.Length;
                        base64Builder.Append(line.Substring(idx).Trim());
                        photoSection = true;
                        continue;
                    }
                    // v2.1/v3.0: PHOTO;ENCODING=BASE64;TYPE=image/png:...
                    int colonIdx = line.IndexOf(':');
                    if (colonIdx >= 0)
                    {
                        string afterColon = line.Substring(colonIdx + 1).Trim();
                        if (!string.IsNullOrEmpty(afterColon))
                        {
                            base64Builder.Append(afterColon);
                        }
                    }
                    photoSection = true;
                    continue;
                }
                if (photoSection)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        photoSection = false;
                    }
                    else if (line.StartsWith(" ") || line.StartsWith("\t"))
                    {
                        base64Builder.Append(line.Trim());
                    }
                    else if (line.Contains(":") || line.Contains(";"))
                    {
                        photoSection = false;
                    }
                    else
                    {
                        base64Builder.Append(line.Trim());
                    }
                }
            }
            string raw = base64Builder.ToString();
            var clean = new StringBuilder();
            foreach (char c in raw)
            {
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