// ======================================================================================================
// VCardix - vCard, CSV and JSON Contact Manager Software
// © Copyright 2025-2026, Eray Türkay.
// Publisher: Türkaysoft
// Project Type: Open Source
// License: MIT License
// Website: https://turkaysoft.com
// GitHub: https://github.com/turkaysoft/vcardix
// ======================================================================================================

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
// TS MODULES
using static VCardix.TSModules;
using static VCardix.VCardixModule;

namespace VCardix{
    public partial class VCardixMain : Form{
        // VCARD MANAGER
        // ======================================================================================================
        readonly VCardixModule VCardManager = new VCardixModule();
        public VCardixMain(){
            InitializeComponent();
            VCardManager.CurrentVersion = VCardVersionChanger(VCardVersion.V30);
            // LANGUAGE SET TAGS
            // ==================
            arabicToolStripMenuItem.Tag = "ar";
            chineseToolStripMenuItem.Tag = "zh";
            englishToolStripMenuItem.Tag = "en";
            dutchToolStripMenuItem.Tag = "nl";
            frenchToolStripMenuItem.Tag = "fr";
            germanToolStripMenuItem.Tag = "de";
            hindiToolStripMenuItem.Tag = "hi";
            italianToolStripMenuItem.Tag = "it";
            japaneseToolStripMenuItem.Tag = "ja";
            koreanToolStripMenuItem.Tag = "ko";
            polishToolStripMenuItem.Tag = "pl";
            portugueseToolStripMenuItem.Tag = "pt";
            russianToolStripMenuItem.Tag = "ru";
            spanishToolStripMenuItem.Tag = "es";
            turkishToolStripMenuItem.Tag = "tr";
            // LANGUAGE SET EVENTS
            // ==================
            arabicToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            chineseToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            englishToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            dutchToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            frenchToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            germanToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            hindiToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            italianToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            japaneseToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            koreanToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            polishToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            portugueseToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            russianToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            spanishToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            turkishToolStripMenuItem.Click += LanguageToolStripMenuItem_Click;
            //
            SystemEvents.UserPreferenceChanged += (s, e) => TSUseSystemTheme();
        }
        // GLOBAL VARIABLES
        // ======================================================================================================
        public static string lang, lang_path;
        public static int theme;
        public static object select_object;
        // LOCAL VARIABLES
        // ======================================================================================================
        private int startup_status, themeSystem;
        private readonly int i_upload_size = 100;
        bool save_status = true;
        // ======================================================================================================
        // COLOR MODES
        static readonly List<Color> header_colors = new List<Color>() { Color.Transparent, Color.Transparent, Color.Transparent };
        // HEADER SETTINGS
        // ======================================================================================================
        private class HeaderMenuColors : ToolStripProfessionalRenderer{
            public HeaderMenuColors() : base(new HeaderColors()) { }
            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor = header_colors[1]; base.OnRenderArrow(e); }
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float dpiScale = g.DpiX / 96f;
                Rectangle rect = e.ImageRectangle;
                using (Pen anti_alias_pen = new Pen(header_colors[2], 2.2f * dpiScale)){
                    anti_alias_pen.StartCap = LineCap.Round;
                    anti_alias_pen.EndCap = LineCap.Round;
                    anti_alias_pen.LineJoin = LineJoin.Round;
                    PointF p1 = new PointF(rect.Left + rect.Width * 0.18f, rect.Top + rect.Height * 0.52f);
                    PointF p2 = new PointF(rect.Left + rect.Width * 0.38f, rect.Top + rect.Height * 0.72f);
                    PointF p3 = new PointF(rect.Left + rect.Width * 0.78f, rect.Top + rect.Height * 0.28f);
                    g.DrawLines(anti_alias_pen, new[] { p1, p2, p3 });
                }
            }
        }
        private class HeaderColors : ProfessionalColorTable{
            public override Color MenuItemSelected => header_colors[0];
            public override Color ToolStripDropDownBackground => header_colors[0];
            public override Color ImageMarginGradientBegin => header_colors[0];
            public override Color ImageMarginGradientEnd => header_colors[0];
            public override Color ImageMarginGradientMiddle => header_colors[0];
            public override Color MenuItemSelectedGradientBegin => header_colors[0];
            public override Color MenuItemSelectedGradientEnd => header_colors[0];
            public override Color MenuItemPressedGradientBegin => header_colors[0];
            public override Color MenuItemPressedGradientMiddle => header_colors[0];
            public override Color MenuItemPressedGradientEnd => header_colors[0];
            public override Color MenuItemBorder => header_colors[0];
            public override Color CheckBackground => header_colors[0];
            public override Color ButtonSelectedBorder => header_colors[0];
            public override Color CheckSelectedBackground => header_colors[0];
            public override Color CheckPressedBackground => header_colors[0];
            public override Color MenuBorder => header_colors[0];
            public override Color SeparatorLight => header_colors[1];
            public override Color SeparatorDark => header_colors[1];
        }
        // LOAD SOFTWARE SETTINGS
        // ======================================================================================================
        private void RunSoftwareEngine(){
            // DOUBLE BUFFER TABLE
            // THEME - LANG - STARTUP MODE PRELOADER
            // ======================================================================================================
            TSSettingsModule software_read_settings = new TSSettingsModule(ts_sf);
            //
            int theme_mode = int.TryParse(software_read_settings.TSReadSettings(ts_settings_container, "ThemeStatus"), out int the_status) && (the_status == 0 || the_status == 1 || the_status == 2) ? the_status : 1;
            if (theme_mode == 2) { themeSystem = 2; Theme_engine(TSThemeModeHelper.GetSystemTheme(2)); } else Theme_engine(theme_mode);
            darkThemeToolStripMenuItem.Checked = theme_mode == 0;
            lightThemeToolStripMenuItem.Checked = theme_mode == 1;
            systemThemeToolStripMenuItem.Checked = theme_mode == 2;
            //
            string lang_mode = software_read_settings.TSReadSettings(ts_settings_container, "LanguageStatus");
            var languageFiles = new Dictionary<string, (object langResource, ToolStripMenuItem menuItem, bool fileExists)>{
                { "ar", (ts_lang_ar, arabicToolStripMenuItem, File.Exists(ts_lang_ar)) },
                { "zh", (ts_lang_zh, chineseToolStripMenuItem, File.Exists(ts_lang_zh)) },
                { "en", (ts_lang_en, englishToolStripMenuItem, File.Exists(ts_lang_en)) },
                { "nl", (ts_lang_nl, dutchToolStripMenuItem, File.Exists(ts_lang_nl)) },
                { "fr", (ts_lang_fr, frenchToolStripMenuItem, File.Exists(ts_lang_fr)) },
                { "de", (ts_lang_de, germanToolStripMenuItem, File.Exists(ts_lang_de)) },
                { "hi", (ts_lang_hi, hindiToolStripMenuItem, File.Exists(ts_lang_hi)) },
                { "it", (ts_lang_it, italianToolStripMenuItem, File.Exists(ts_lang_it)) },
                { "ja", (ts_lang_ja, japaneseToolStripMenuItem, File.Exists(ts_lang_ja)) },
                { "ko", (ts_lang_ko, koreanToolStripMenuItem, File.Exists(ts_lang_ko)) },
                { "pl", (ts_lang_pl, polishToolStripMenuItem, File.Exists(ts_lang_pl)) },
                { "pt", (ts_lang_pt, portugueseToolStripMenuItem, File.Exists(ts_lang_pt)) },
                { "ru", (ts_lang_ru, russianToolStripMenuItem, File.Exists(ts_lang_ru)) },
                { "es", (ts_lang_es, spanishToolStripMenuItem, File.Exists(ts_lang_es)) },
                { "tr", (ts_lang_tr, turkishToolStripMenuItem, File.Exists(ts_lang_tr)) },
            };
            foreach (var langLoader in languageFiles) { langLoader.Value.menuItem.Enabled = langLoader.Value.fileExists; }
            var (langResource, selectedMenuItem, _) = languageFiles.ContainsKey(lang_mode) ? languageFiles[lang_mode] : languageFiles["en"];
            Lang_engine(Convert.ToString(langResource), lang_mode);
            selectedMenuItem.Checked = true;
            //
            string startup_mode = software_read_settings.TSReadSettings(ts_settings_container, "StartupStatus");
            startup_status = int.TryParse(startup_mode, out int str_status) && (str_status == 0 || str_status == 1) ? str_status : 0;
            WindowState = startup_status == 1 ? FormWindowState.Maximized : FormWindowState.Normal;
            windowedToolStripMenuItem.Checked = startup_status == 0;
            fullScreenToolStripMenuItem.Checked = startup_status == 1;
            //
            BtnOpenAdressWindow.Height = textBoxAddress.Height + 2;
        }
        // MAIN TOOLTIP SETTINGS
        // ======================================================================================================
        private void MainToolTip_Draw(object sender, DrawToolTipEventArgs e){ e.DrawBackground(); e.DrawBorder(); e.DrawText(); }
        // LOAD
        // ====================================================================================================== 
        private void VCardix_Load(object sender, EventArgs e){
            Text = TS_VersionEngine.TS_SofwareVersion(0);
            HeaderMenu.Cursor = Cursors.Hand;
            CXImageMenu.Cursor = Cursors.Hand;
            RunSoftwareEngine();
            //
            Task softwareUpdateCheck = Task.Run(() => Software_update_check(0));
        }
        // GET PERSON DATA FROM INPUTS
        // ====================================================================================================== 
        private PrefixModule GetContactFromInputs(){
            var selectedContact = ContactList.SelectedItem as PrefixModule;
            var contact = new PrefixModule{
                Id = selectedContact?.Id ?? Guid.NewGuid(),
                FirstName = textBoxFirstName.Text.Trim(),
                MiddleName = textBoxMiddleName.Text.Trim(),
                LastName = textBoxLastName.Text.Trim(),
                Birthday = dateTimePickerBirthday.Checked ? (DateTime?)dateTimePickerBirthday.Value.Date : null,
                PhoneMobile = textBoxPhoneMobile.Text.Trim(),
                PhoneHome = textBoxPhoneHome.Text.Trim(),
                PhoneWork = textBoxPhoneWork.Text.Trim(),
                Email1 = textBoxEmail1.Text.Trim(),
                Email2 = textBoxEmail2.Text.Trim(),
                Email3 = textBoxEmail3.Text.Trim(),
                Address = textBoxAddress.Text.Trim(),
                Organization = textBoxOrganization.Text.Trim(),
                Website = textBoxWebsite.Text.Trim(),
                Note = textBoxNote.Text.Trim(),
                PhotoBase64 = selectedContact?.PhotoBase64
            };
            return contact;
        }
        // REFRESH CONTACT LIST
        // ====================================================================================================== 
        private void RefreshList(){
            CleanUI();
            ContactList.Items.Clear();
            ContactList.Items.AddRange(VCardManager.ContactsList.OrderBy(c => TSNaturalSortKey(c.FullName ?? string.Empty)).ToArray());
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (ContactList.Items.Count > 0){
                BottomInfoLabel.Text = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_bottom_ready"), ContactList.Items.Count.ToString());
            }else{
                BottomInfoLabel.Text = software_lang.TSReadLangs("VCardixUI", "vcui_bottom_ready_empty");
            }
        }
        // CLEAR UI
        // ====================================================================================================== 
        private void CleanUI(){
            textBoxSearch.Text = string.Empty;
            //
            textBoxFirstName.Text = string.Empty;
            textBoxMiddleName.Text = string.Empty;
            textBoxLastName.Text = string.Empty;
            //
            dateTimePickerBirthday.Value = DateTime.Now;
            dateTimePickerBirthday.Checked = false;
            //
            textBoxPhoneMobile.Text = string.Empty;
            textBoxPhoneHome.Text = string.Empty;
            textBoxPhoneWork.Text = string.Empty;
            //
            textBoxEmail1.Text = string.Empty;
            textBoxEmail2.Text = string.Empty;
            textBoxEmail3.Text = string.Empty;
            //
            textBoxAddress.Text = string.Empty;
            textBoxOrganization.Text = string.Empty;
            textBoxWebsite.Text = string.Empty;
            textBoxNote.Text = string.Empty;
            //
            TSImageHelper.SetPictureBoxImage(ContactUserImage, null);
        }
        // CONTACT PERSON CHANGE
        // ====================================================================================================== 
        private void ContactList_SelectedIndexChanged(object sender, EventArgs e){
            if (ContactList.SelectedItem is PrefixModule c){
                //
                select_object = ContactList.SelectedItem;
                //
                textBoxFirstName.Text = c.FirstName;
                textBoxMiddleName.Text = c.MiddleName;
                textBoxLastName.Text = c.LastName;
                //
                if (c.Birthday.HasValue){
                    dateTimePickerBirthday.Value = c.Birthday.Value;
                    dateTimePickerBirthday.Checked = true;
                }else{
                    dateTimePickerBirthday.Value = DateTime.Now;
                    dateTimePickerBirthday.Checked = false;
                }
                //
                textBoxPhoneMobile.Text = c.PhoneMobile;
                textBoxPhoneHome.Text = c.PhoneHome;
                textBoxPhoneWork.Text = c.PhoneWork;
                //
                textBoxEmail1.Text = c.Email1;
                textBoxEmail2.Text = c.Email2;
                textBoxEmail3.Text = c.Email3;
                //
                textBoxAddress.Text = c.Address;
                textBoxOrganization.Text = c.Organization;
                textBoxWebsite.Text = c.Website;
                textBoxNote.Text = c.Note;
                //
                TSImageHelper.SetPictureBoxImage(ContactUserImage, c.PhotoImage ?? TSImageHelper.ImageFromBase64(c.PhotoBase64));
            }else{
                TSImageHelper.SetPictureBoxImage(ContactUserImage, null);
            }
        }
        // ADD NEW PERSON
        // ====================================================================================================== 
        private void BtnAdd_Click(object sender, EventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (!string.IsNullOrEmpty(textBoxFirstName.Text.Trim())){
                VCardManager.AddContact(GetContactFromInputs());
                RefreshList();
                CleanUI();
                save_status = false;
                TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_new_person_add_success"));
            }else{
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_new_person_add_warning"));
            }
        }
        // UPDATE PERSON
        // ====================================================================================================== 
        private void BtnUpdate_Click(object sender, EventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (ContactList.SelectedItem is PrefixModule selected){
                DialogResult update_warning = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_update_person_query"), selected.FullName));
                if (update_warning == DialogResult.Yes){
                    if (VCardManager.UpdateContact(selected.Id, GetContactFromInputs())){
                        RefreshList();
                        CleanUI();
                        save_status = false;
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_update_person_success"));
                    }else{
                        TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_person_not_found"));
                    }
                }
            }else{
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_update_person_process_warning"));
            }
        }
        // DELETE PERSON
        // ====================================================================================================== 
        private void BtnDelete_Click(object sender, EventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (ContactList.SelectedItem is PrefixModule selected){
                var delete_warning = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_delete_person_query"), selected.FullName));
                if (delete_warning == DialogResult.Yes){
                    try{
                        VCardManager.DeleteContact(selected.Id);
                        RefreshList();
                        CleanUI();
                        save_status = false;
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_delete_person_success"));
                    }catch (Exception ex){
                        TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_delete_failed"), "\n\n", ex.Message));
                    }
                }
            }else{
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_delete_person_process_warning"));
            }
        }
        // ADD PERSON IMAGE
        // ====================================================================================================== 
        private void ContactUserImage_MouseUp(object sender, MouseEventArgs e){
            if (e.Button != MouseButtons.Right && e.Button != MouseButtons.Left) { return; }
            //
            cxImageSelectToolStripMenuItem.Enabled = ContactList.SelectedItem is PrefixModule;
            bool hasImage = ContactUserImage.Image != null;
            cxViewImageToolStripMenuItem.Enabled = hasImage;
            cxRemoveImageToolStripMenuItem.Enabled = hasImage;
            cxSaveImageToolStripMenuItem.Enabled = hasImage;
            //
            CXImageMenu.Show(ContactUserImage, e.Location);
        }
        private void CxImageSelectToolStripMenuItem_Click(object sender, EventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            bool imageSelectMode = false;
            if (ContactUserImage.Image != null){
                DialogResult image_change_query = TS_MessageBoxEngine.TS_MessageBox(this, 5, software_lang.TSReadLangs("VCardixUI", "vcui_image_change_info"));
                if (image_change_query == DialogResult.Yes){
                    imageSelectMode = true;
                }
            }else{
                imageSelectMode = true;
            }
            if (imageSelectMode){
                using (OpenFileDialog ofd = new OpenFileDialog()){
                    ofd.Title = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_select_image"), Application.ProductName);
                    ofd.Filter = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_image_filter"), "|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff");
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    ofd.RestoreDirectory = true;
                    ofd.CheckFileExists = true;
                    ofd.Multiselect = false;
                    if (ofd.ShowDialog() == DialogResult.OK){
                        AddImageToSelectedContact(ofd.FileName, imageSelectMode);
                    }
                }
            }
        }
        private void CxSaveImageToolStripMenuItem_Click(object sender, EventArgs e){
            if (ContactList.SelectedItem is PrefixModule c){
                SaveContentImage(TSImageHelper.ImageFromBase64(c.PhotoBase64), this);
            }
        }
        public static void SaveContentImage(Image imgSrc, Form owner){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            try{
                using (SaveFileDialog sfd = new SaveFileDialog()){
                    sfd.Title = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_image_save_title"), Application.ProductName);
                    sfd.Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp|TIFF (*.tiff)|*.tiff";
                    sfd.DefaultExt = "png";
                    sfd.AddExtension = true;
                    sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    sfd.FileName = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_name"), Application.ProductName, DateTime.Now.ToString("ddMMyyyy_HHmm"), Path.GetRandomFileName().Replace(".", "").Substring(0, 7));
                    if (sfd.ShowDialog() == DialogResult.OK){
                        string ext = Path.GetExtension(sfd.FileName).ToLower();
                        ImageFormat format = ImageFormat.Png;
                        switch (ext){
                            case ".jpg":
                            case ".jpeg":
                                format = ImageFormat.Jpeg;
                                break;
                            case ".bmp":
                                format = ImageFormat.Bmp;
                                break;
                            case ".tiff":
                                format = ImageFormat.Tiff;
                                break;
                            case ".png":
                            default:
                                format = ImageFormat.Png;
                                break;
                        }
                        imgSrc.Save(sfd.FileName, format);
                        DialogResult ofi_location = TS_MessageBoxEngine.TS_MessageBox(owner, 5, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_image_save_success"), sfd.FileName, "\n\n"));
                        if (ofi_location == DialogResult.Yes){
                            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{sfd.FileName}\"") { UseShellExecute = true });
                        }
                    }
                }
            }catch (Exception ex){
                TS_MessageBoxEngine.TS_MessageBox(owner, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_image_save_failed"), "\n\n", ex.Message));
            }
        }
        private void CxRemoveImageToolStripMenuItem_Click(object sender, EventArgs e){
            if (ContactList.SelectedItem is PrefixModule selected){
                selected.ClearPhoto();
                TSImageHelper.SetPictureBoxImage(ContactUserImage, null);
                TSGetLangs software_lang = new TSGetLangs(lang_path);
                TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_image_remove_success"));
            }
        }
        private void AddImageToSelectedContact(string imagePath, bool imageMode){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (ContactList.Items.Count == 0){
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_image_person_not_added_warning"));
                return;
            }
            if (!(ContactList.SelectedItem is PrefixModule selected)){
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_image_person_not_select_warning"));
                return;
            }
            FileInfo fi = new FileInfo(imagePath);
            long maxFileSize = i_upload_size * 1024 * 1024;
            if (fi.Length > maxFileSize){
                TS_MessageBoxEngine.TS_MessageBox(this, 2, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_image_size_limit_warning"), i_upload_size));
                return;
            }
            try{
                using (var originalImage = Image.FromFile(imagePath))
                using (var ms = new MemoryStream()){
                    originalImage.Save(ms, ImageFormat.Png);
                    string base64 = Convert.ToBase64String(ms.ToArray());
                    selected.ClearPhoto();
                    selected.PhotoBase64 = base64;
                    TSImageHelper.SetPictureBoxImage(ContactUserImage, selected.PhotoImage);
                    VCardManager.UpdateContact(selected.Id, selected);
                    save_status = false;
                    if (!imageMode)
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_image_import_success"));
                    else
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_image_update_success"));
                }
            }catch (Exception ex){
                TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_image_import_failed"), "\n\n", ex.Message));
            }
        }
        // DYNAMIC SEARCH
        // ====================================================================================================== 
        private void TextBoxSearch_TextChanged(object sender, EventArgs e){
            try{
                var results = VCardManager.SearchContacts(textBoxSearch.Text.Trim());
                ContactList.Items.Clear();
                ContactList.Items.AddRange(results.OrderBy(c => TSNaturalSortKey(c.FullName)).ToArray());
            }catch (Exception){ }
        }
        // vCARD VERSION CHANGER
        // ====================================================================================================== 
        private VCardVersion VCardVersionChanger(VCardVersion? setVersion = null){
            if (setVersion.HasValue){
                vcard21ToolStripMenuItem.Checked = (setVersion.Value == VCardVersion.V21);
                vcard30ToolStripMenuItem.Checked = (setVersion.Value == VCardVersion.V30);
                vcard40ToolStripMenuItem.Checked = (setVersion.Value == VCardVersion.V40);
                return setVersion.Value;
            }else{
                if (vcard21ToolStripMenuItem.Checked) return VCardVersion.V21;
                if (vcard30ToolStripMenuItem.Checked) return VCardVersion.V30;
                if (vcard40ToolStripMenuItem.Checked) return VCardVersion.V40;
                return VCardVersion.V30;
            }
        }
        // HEADER vCARD VERSION CHANGER
        // ====================================================================================================== 
        private void Vcard21ToolStripMenuItem_Click(object sender, EventArgs e){
            VCardManager.CurrentVersion = VCardVersion.V21;
            UpdateMenuChecks(vcard21ToolStripMenuItem);
        }
        private void Vcard30ToolStripMenuItem_Click(object sender, EventArgs e){
            VCardManager.CurrentVersion = VCardVersion.V30;
            UpdateMenuChecks(vcard30ToolStripMenuItem);
        }
        private void Vcard40ToolStripMenuItem_Click(object sender, EventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            DialogResult vcard_40_warning = TS_MessageBoxEngine.TS_MessageBox(this, 6, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_40_change_warning"), "\n\n"));
            if (vcard_40_warning == DialogResult.Yes){
                VCardManager.CurrentVersion = VCardVersion.V40;
                UpdateMenuChecks(vcard40ToolStripMenuItem);
            }
        }
        private void UpdateMenuChecks(ToolStripMenuItem selectedItem){
            vcard21ToolStripMenuItem.Checked = false;
            vcard30ToolStripMenuItem.Checked = false;
            vcard40ToolStripMenuItem.Checked = false;
            selectedItem.Checked = true;
        }
        // SORTING MODE CHANGER
        // ====================================================================================================== 
        private void SortingFullNameToolStripMenuItem_Click(object sender, EventArgs e){
            UpdateSortringMenuChecks(sortingFullNameToolStripMenuItem);
            RefreshContactList("FullName", c => c.FullName);
        }
        private void SortingFirstNameToolStripMenuItem_Click(object sender, EventArgs e){
            UpdateSortringMenuChecks(sortingFirstNameToolStripMenuItem);
            RefreshContactList("FirstName", c => c.FirstName);
        }
        private void SortingLastNameToolStripMenuItem_Click(object sender, EventArgs e){
            UpdateSortringMenuChecks(sortingLastNameToolStripMenuItem);
            RefreshContactList("LastName", c => c.LastName);
        }
        private void SortingPhoneToolStripMenuItem_Click(object sender, EventArgs e){
            UpdateSortringMenuChecks(sortingPhoneToolStripMenuItem);
            RefreshContactList("PhoneMobile", c => c.PhoneMobile);
        }
        private void RefreshContactList(string displayMember, Func<PrefixModule, string> orderBySelector){
            var selectedContact = ContactList.SelectedItem as PrefixModule;
            Guid? selectedId = selectedContact?.Id;
            var sortedItems = VCardManager.ContactsList.OrderBy(c => TSNaturalSortKey(orderBySelector(c))).ToList();
            foreach (var contact in sortedItems){
                contact.CurrentDisplayMember = displayMember;
            }
            ContactList.BeginUpdate();
            ContactList.Items.Clear();
            ContactList.Items.AddRange(sortedItems.ToArray());
            if (selectedId.HasValue){
                for (int i = 0; i < ContactList.Items.Count; i++){
                    if (((PrefixModule)ContactList.Items[i]).Id == selectedId.Value){
                        ContactList.SelectedIndex = i;
                        break;
                    }
                }
            }
            ContactList.EndUpdate();
        }
        private void UpdateSortringMenuChecks(ToolStripMenuItem selectedItem){
            sortingFullNameToolStripMenuItem.Checked = false;
            sortingFirstNameToolStripMenuItem.Checked = false;
            sortingLastNameToolStripMenuItem.Checked = false;
            sortingPhoneToolStripMenuItem.Checked = false;
            selectedItem.Checked = true;
        }
        // IMPORT FILE & DRAG AND DROP
        // ====================================================================================================== 
        private void VCardix_DragEnter(object sender, DragEventArgs e){
            if (e.Data.GetDataPresent(DataFormats.FileDrop)){
                e.Effect = DragDropEffects.Copy;
            }
        }
        private async void VCardix_DragDrop(object sender, DragEventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] drop_files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (drop_files.Length == 0) return;
            if (drop_files.Length > 1){
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_single_file_warning"));
                return;
            }
            string drop_file = drop_files[0];
            string ext = Path.GetExtension(drop_file).ToLower();
            string[] imageExtensions = { ".bmp", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
            try{
                if (imageExtensions.Contains(ext)){
                    if (ContactUserImage.Image == null){
                        AddImageToSelectedContact(drop_file, false);
                        return;
                    }else{
                        DialogResult image_change_query = TS_MessageBoxEngine.TS_MessageBox(this, 5, software_lang.TSReadLangs("VCardixUI", "vcui_image_change_info"));
                        if (image_change_query == DialogResult.Yes){
                            AddImageToSelectedContact(drop_file, true);
                        }
                        return;
                    }
                }
                if (ext == ".vcf" || ext == ".csv" || ext == ".json"){
                    if (ContactList.Items.Count > 0){
                        var import_warning = TS_MessageBoxEngine.TS_MessageBox(this, 6, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_import_warning"), "\n\n"));
                        if (import_warning != DialogResult.Yes){ return; }
                    }
                    LoadSaveTitle(0);
                    if (ext == ".vcf"){
                        await Task.Run(() => VCardManager.LoadVcf(drop_file));
                        VCardVersionChanger(VCardManager.CurrentVersion);
                        RefreshList();
                        LoadSaveTitle(2);
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_vcard_import_success"));
                    }else if (ext == ".csv"){
                        await Task.Run(() => VCardManager.LoadCsv(drop_file));
                        RefreshList();
                        LoadSaveTitle(2);
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_csv_import_success"));
                    }else if (ext == ".json"){
                        await Task.Run(() => VCardManager.LoadJson(drop_file));
                        VCardVersionChanger(VCardManager.CurrentVersion);
                        RefreshList();
                        LoadSaveTitle(2);
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_json_import_success"));
                    }
                    //save_status = false;
                    return;
                }
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_dad_filter"));
            }catch (Exception ex){
                TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_dad_failed"), "\n\n", ex.Message));
            }
        }
        private async void OpenAndLoadFile(){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            using (var ofg = new OpenFileDialog()){
                ofg.Title = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_select_file"), Application.ProductName);
                ofg.Filter = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_select_filter"), "(*.vcf;*.csv;*.json)|*.vcf;*.csv;*.json");
                ofg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                ofg.RestoreDirectory = true;
                ofg.CheckFileExists = true;
                ofg.Multiselect = false;
                if (ofg.ShowDialog() == DialogResult.OK){
                    try{
                        string ext = Path.GetExtension(ofg.FileName).ToLower();
                        LoadSaveTitle(0);
                        if (ext == ".vcf"){
                            await Task.Run(() => VCardManager.LoadVcf(ofg.FileName));
                            VCardVersionChanger(VCardManager.CurrentVersion);
                            RefreshList();
                            LoadSaveTitle(2);
                            TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_vcard_import_success"));
                        }else if (ext == ".csv"){
                            await Task.Run(() => VCardManager.LoadCsv(ofg.FileName));
                            RefreshList();
                            LoadSaveTitle(2);
                            TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_csv_import_success"));
                        }else if (ext == ".json"){
                            await Task.Run(() => VCardManager.LoadJson(ofg.FileName));
                            RefreshList();
                            LoadSaveTitle(2);
                            TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("VCardixUI", "vcui_json_import_success"));
                        }
                        //save_status = false;
                    }catch (Exception ex){
                        TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_select_failed"), "\n\n", ex.Message));
                    }
                }
            }
        }
        // EXPORT FILE
        // ====================================================================================================== 
        private async void SaveContactsToFile(){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (ContactList.Items.Count == 0){
                TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("VCardixUI", "vcui_export_warning"));
                return;
            }
            using (var dlg = new SaveFileDialog()){
                dlg.FileName = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_name"), Application.ProductName, DateTime.Now.ToString("ddMMyyyy_HHmm"), Path.GetRandomFileName().Replace(".", "").Substring(0, 7));
                dlg.Title = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_file"), Application.ProductName);
                dlg.Filter = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_filter"), "(*.vcf)|*.vcf|", "(*.csv)|*.csv|", "(*.json)|*.json");
                dlg.DefaultExt = "vcf";
                dlg.RestoreDirectory = true;
                dlg.OverwritePrompt = true;
                dlg.AddExtension = true;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (dlg.ShowDialog() == DialogResult.OK){
                    try{
                        string ext = Path.GetExtension(dlg.FileName).ToLower();
                        LoadSaveTitle(1);
                        if (ext == ".vcf"){
                            if (VCardManager.CurrentVersion == VCardVersion.V40){
                                DialogResult warning_v40 = TS_MessageBoxEngine.TS_MessageBox(this, 6, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_40_warning"), "\n\n"));
                                if (warning_v40 == DialogResult.Yes){
                                    VCardManager.CurrentVersion = VCardVersionChanger();
                                }else{
                                    return;
                                }
                            }else{
                                VCardManager.CurrentVersion = VCardVersionChanger();
                            }
                            await Task.Run(() => VCardManager.SaveVcf(dlg.FileName));
                            LoadSaveTitle(2);
                            DialogResult save_aof_warning = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_vcard_export_success"), dlg.FileName, "\n\n"));
                            if (save_aof_warning == DialogResult.Yes){
                                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{dlg.FileName}\"") { UseShellExecute = true });
                            }
                        }else if (ext == ".csv"){
                            await Task.Run(() => VCardManager.SaveCsv(dlg.FileName));
                            LoadSaveTitle(2);
                            DialogResult save_aof_warning = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_csv_export_success"), dlg.FileName, "\n\n"));
                            if (save_aof_warning == DialogResult.Yes){
                                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{dlg.FileName}\"") { UseShellExecute = true });
                            }
                        }else if (ext == ".json"){
                            await Task.Run(() => VCardManager.SaveJson(dlg.FileName));
                            LoadSaveTitle(2);
                            DialogResult save_aof_warning = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_json_export_success"), dlg.FileName, "\n\n"));
                            if (save_aof_warning == DialogResult.Yes){
                                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{dlg.FileName}\"") { UseShellExecute = true });
                            }
                        }
                        save_status = true;
                    }catch (Exception ex){
                        TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_failed"), "\n\n", ex.Message));
                    }
                }
            }
        }
        // IMPORT HEADER BTN CLICK
        // ====================================================================================================== 
        private void ImportFileToolStripMenuItem_Click(object sender, EventArgs e){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            if (ContactList.Items.Count > 0){
                var import_warning = TS_MessageBoxEngine.TS_MessageBox(this, 6, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_import_warning"), "\n\n"));
                if (import_warning == DialogResult.Yes){
                    OpenAndLoadFile();
                }
            }else{
                OpenAndLoadFile();
            }
        }
        // EXPORT HEADER BTN CLICK
        // ====================================================================================================== 
        private void ExportFileToolStripMenuItem_Click(object sender, EventArgs e){
            SaveContactsToFile();
        }
        // LOAD & SAVE TITLE CHANGER
        // ====================================================================================================== 
        private void LoadSaveTitle(int mode){
            TSGetLangs software_lang = new TSGetLangs(lang_path);
            string main_title = TS_VersionEngine.TS_SofwareVersion(0);
            if (mode == 0){
                Text = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_load"), main_title);
            }else if (mode == 1){
                Text = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_save"), main_title);
            }else if (mode == 2){
                Text = main_title;
            }
        }
        // THEME MODE
        // ======================================================================================================
        private ToolStripMenuItem selected_theme = null;
        private void Select_theme_active(object target_theme){
            if (target_theme == null)
                return;
            ToolStripMenuItem clicked_theme = (ToolStripMenuItem)target_theme;
            if (selected_theme == clicked_theme)
                return;
            Select_theme_deactive();
            selected_theme = clicked_theme;
            selected_theme.Checked = true;
        }
        private void Select_theme_deactive(){
            foreach (ToolStripMenuItem theme in themeToolStripMenuItem.DropDownItems){
                theme.Checked = false;
            }
        }
        // THEME SWAP
        // ======================================================================================================
        private void SystemThemeToolStripMenuItem_Click(object sender, EventArgs e){
            themeSystem = 2; Theme_engine(TSThemeModeHelper.GetSystemTheme(2)); SaveTheme(2); Select_theme_active(sender);
        }
        private void LightThemeToolStripMenuItem_Click(object sender, EventArgs e){
            themeSystem = 0; Theme_engine(1); SaveTheme(1); Select_theme_active(sender);
        }
        private void DarkThemeToolStripMenuItem_Click(object sender, EventArgs e){
            themeSystem = 0; Theme_engine(0); SaveTheme(0); Select_theme_active(sender);
        }
        private void TSUseSystemTheme() { if (themeSystem == 2) Theme_engine(TSThemeModeHelper.GetSystemTheme(2)); }
        private void SaveTheme(int ts){
            // SAVE CURRENT THEME
            try{
                TSSettingsModule software_setting_save = new TSSettingsModule(ts_sf);
                software_setting_save.TSWriteSettings(ts_settings_container, "ThemeStatus", Convert.ToString(ts));
            }catch (Exception){ }
        }
        // THEME ENGINE
        // ======================================================================================================
        private void Theme_engine(int ts){
            try{
                theme = ts;
                //
                TSThemeModeHelper.SetThemeMode(ts == 0);
                TSThemeModeHelper.InitializeThemeForForm(this);
                //
                if (theme == 1){
                    // FILE
                    TSImageRenderer(fileToolStripMenuItem, Properties.Resources.tm_file_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(importFileToolStripMenuItem, Properties.Resources.tm_import_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(exportFileToolStripMenuItem, Properties.Resources.tm_export_light, 0, ContentAlignment.MiddleRight);
                    // VCARD VERSION
                    TSImageRenderer(vCardVersionToolStripMenuItem, Properties.Resources.tm_vcard_light, 0, ContentAlignment.MiddleRight);
                    // SORTING MODE
                    TSImageRenderer(sortingModeToolStripMenuItem, Properties.Resources.tm_sorting_mode_light, 0, ContentAlignment.MiddleRight);
                    // SETTINGS
                    TSImageRenderer(settingsToolStripMenuItem, Properties.Resources.tm_settings_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(themeToolStripMenuItem, Properties.Resources.tm_theme_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(languageToolStripMenuItem, Properties.Resources.tm_language_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(startupToolStripMenuItem, Properties.Resources.tm_startup_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(checkForUpdateToolStripMenuItem, Properties.Resources.tm_update_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(donateToolStripMenuItem, Properties.Resources.tm_donate_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(aboutToolStripMenuItem, Properties.Resources.tm_about_light, 0, ContentAlignment.MiddleRight);
                    //
                    TSImageRenderer(BtnAdd, Properties.Resources.ct_add_light, 18, ContentAlignment.MiddleLeft);
                    TSImageRenderer(BtnUpdate, Properties.Resources.ct_update_light, 18, ContentAlignment.MiddleLeft);
                    TSImageRenderer(BtnDelete, Properties.Resources.ct_delete_light, 18, ContentAlignment.MiddleLeft);
                    //
                    TSImageRenderer(BtnOpenAdressWindow, Properties.Resources.ct_adress_window_light, 13, ContentAlignment.MiddleCenter);
                    // PHOTO MODE
                    TSImageRenderer(cxImageSelectToolStripMenuItem, Properties.Resources.ct_select_image_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(cxViewImageToolStripMenuItem, Properties.Resources.ct_show_image_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(cxSaveImageToolStripMenuItem, Properties.Resources.ct_save_image_light, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(cxRemoveImageToolStripMenuItem, Properties.Resources.ct_delete_image_light, 0, ContentAlignment.MiddleRight);
                }else if (theme == 0){
                    // FILE
                    TSImageRenderer(fileToolStripMenuItem, Properties.Resources.tm_file_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(importFileToolStripMenuItem, Properties.Resources.tm_import_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(exportFileToolStripMenuItem, Properties.Resources.tm_export_dark, 0, ContentAlignment.MiddleRight);
                    // VCARD VERSION
                    TSImageRenderer(vCardVersionToolStripMenuItem, Properties.Resources.tm_vcard_dark, 0, ContentAlignment.MiddleRight);
                    // SORTING MODE
                    TSImageRenderer(sortingModeToolStripMenuItem, Properties.Resources.tm_sorting_mode_dark, 0, ContentAlignment.MiddleRight);
                    // SETTINGS
                    TSImageRenderer(settingsToolStripMenuItem, Properties.Resources.tm_settings_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(themeToolStripMenuItem, Properties.Resources.tm_theme_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(languageToolStripMenuItem, Properties.Resources.tm_language_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(startupToolStripMenuItem, Properties.Resources.tm_startup_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(checkForUpdateToolStripMenuItem, Properties.Resources.tm_update_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(donateToolStripMenuItem, Properties.Resources.tm_donate_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(aboutToolStripMenuItem, Properties.Resources.tm_about_dark, 0, ContentAlignment.MiddleRight);
                    //
                    TSImageRenderer(BtnAdd, Properties.Resources.ct_add_dark, 18, ContentAlignment.MiddleLeft);
                    TSImageRenderer(BtnUpdate, Properties.Resources.ct_update_dark, 18, ContentAlignment.MiddleLeft);
                    TSImageRenderer(BtnDelete, Properties.Resources.ct_delete_dark, 18, ContentAlignment.MiddleLeft);
                    //
                    TSImageRenderer(BtnOpenAdressWindow, Properties.Resources.ct_adress_window_dark, 13, ContentAlignment.MiddleCenter);
                    // PHOTO MODE
                    TSImageRenderer(cxImageSelectToolStripMenuItem, Properties.Resources.ct_select_image_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(cxViewImageToolStripMenuItem, Properties.Resources.ct_show_image_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(cxSaveImageToolStripMenuItem, Properties.Resources.ct_save_image_dark, 0, ContentAlignment.MiddleRight);
                    TSImageRenderer(cxRemoveImageToolStripMenuItem, Properties.Resources.ct_delete_image_dark, 0, ContentAlignment.MiddleRight);
                }
                // HEADER
                header_colors[0] = TS_ThemeEngine.ColorMode(theme, "HeaderBGColorMain");
                header_colors[1] = TS_ThemeEngine.ColorMode(theme, "HeaderFEColorMain");
                header_colors[2] = TS_ThemeEngine.ColorMode(theme, "AccentColor");
                HeaderMenu.Renderer = new HeaderMenuColors();
                CXImageMenu.Renderer = new HeaderMenuColors();
                // TOOLTIP
                MainToolTip.ForeColor = TS_ThemeEngine.ColorMode(theme, "HeaderFEColor");
                MainToolTip.BackColor = TS_ThemeEngine.ColorMode(theme, "HeaderBGColor");
                // HEADER MENU
                // ===========================================
                var bg = TS_ThemeEngine.ColorMode(theme, "HeaderBGColor");
                var fg = TS_ThemeEngine.ColorMode(theme, "HeaderFEColor");
                HeaderMenu.ForeColor = fg;
                HeaderMenu.BackColor = bg;
                SetMenuStripColors(HeaderMenu, bg, fg);
                SetContextMenuColors(CXImageMenu, bg, fg);
                // UI
                BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor2");
                //
                lblSearch.ForeColor = TS_ThemeEngine.ColorMode(theme, "AccentColorText");
                //
                ContactList.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor");
                ContactList.ForeColor = TS_ThemeEngine.ColorMode(theme, "AccentColorText");
                ContactList.SelectedBackColor = TS_ThemeEngine.ColorMode(theme, "AccentColor");
                ContactList.SelectedForeColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor");
                //
                var combinedBtnsControls = BackPanel.Controls.Cast<Control>().Concat(UIPanel.Controls.Cast<Control>());
                foreach (Control ui_controls in combinedBtnsControls){
                    if (ui_controls is Button ui_btn){
                        ui_btn.ForeColor = TS_ThemeEngine.ColorMode(theme, "DynamicThemeActiveBtnBG");
                        ui_btn.BackColor = TS_ThemeEngine.ColorMode(theme, "AccentColor");
                        ui_btn.FlatAppearance.BorderColor = TS_ThemeEngine.ColorMode(theme, "AccentColor");
                        ui_btn.FlatAppearance.MouseDownBackColor = TS_ThemeEngine.ColorMode(theme, "AccentColor");
                        ui_btn.FlatAppearance.MouseOverBackColor = TS_ThemeEngine.ColorMode(theme, "AccentColorHover");
                    }
                    if (ui_controls is TextBox ui_textbox){
                        ui_textbox.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor2");
                        ui_textbox.ForeColor = TS_ThemeEngine.ColorMode(theme, "AccentColorText");
                    }
                    if (ui_controls is Label ui_label){
                        ui_label.ForeColor = TS_ThemeEngine.ColorMode(theme, "AccentColorText");
                    }
                }
                textBoxSearch.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor");
                textBoxSearch.ForeColor = TS_ThemeEngine.ColorMode(theme, "AccentColorText");
                //
                BottomInfoLabel.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor");
                //
                UIPanel.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor");
                //
                dateTimePickerBirthday.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor2");
                dateTimePickerBirthday.ButtonColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor");
                dateTimePickerBirthday.ForeColor = TS_ThemeEngine.ColorMode(theme, "AccentColorText");
                dateTimePickerBirthday.BorderColor = TS_ThemeEngine.ColorMode(theme, "BorderColor");
                dateTimePickerBirthday.FocusedBorderColor = TS_ThemeEngine.ColorMode(theme, "BorderColor");
                //
                ContactUserImage.BackColor = TS_ThemeEngine.ColorMode(theme, "UIBGColor2");
                //
                Software_other_page_preloader();
            }catch (Exception){ }
        }
        private void SetMenuStripColors(MenuStrip menuStrip, Color bgColor, Color fgColor){
            if (menuStrip == null) return;
            foreach (ToolStripItem item in menuStrip.Items){
                if (item is ToolStripMenuItem menuItem){
                    SetMenuItemColors(menuItem, bgColor, fgColor);
                }
            }
        }
        private void SetMenuItemColors(ToolStripMenuItem menuItem, Color bgColor, Color fgColor){
            if (menuItem == null) return;
            menuItem.BackColor = bgColor;
            menuItem.ForeColor = fgColor;
            foreach (ToolStripItem item in menuItem.DropDownItems){
                if (item is ToolStripMenuItem subMenuItem){
                    SetMenuItemColors(subMenuItem, bgColor, fgColor);
                }
            }
        }
        private void SetContextMenuColors(ContextMenuStrip contextMenu, Color bgColor, Color fgColor){
            if (contextMenu == null) return;
            foreach (ToolStripItem item in contextMenu.Items){
                if (item is ToolStripMenuItem menuItem){
                    SetMenuItemColors(menuItem, bgColor, fgColor);
                }
            }
        }
        // OTHER PAGE DYNAMIC UI
        private void OtherDynamicUI<T>(string formName, Action<T> preloader) where T : Form, new(){
            try{
                T formInstance;
                if (Application.OpenForms[formName] != null){
                    formInstance = (T)Application.OpenForms[formName];
                    preloader(formInstance);
                }else{
                    formInstance = new T{ Name = formName };
                }
            }catch (Exception){ }
        }
        private void Software_other_page_preloader(){
            OtherDynamicUI<VCardixAdressWindow>("vcardix_adress_window", f => f.Adress_window_preloader());
            OtherDynamicUI<VCardixImagePreview>("vcardix_image_preview", f => f.Image_preview_preloader());
            OtherDynamicUI<VCardixAbout>("vcardix_about", f => f.About_preloader());
        }
        // LANG MODE
        // ======================================================================================================
        private void Select_lang_active(object target_lang){
            ToolStripMenuItem selected_lang = null;
            Select_lang_deactive();
            if (target_lang != null){
                if (selected_lang != (ToolStripMenuItem)target_lang){
                    selected_lang = (ToolStripMenuItem)target_lang;
                    selected_lang.Checked = true;
                }
            }
        }
        private void Select_lang_deactive(){
            foreach (ToolStripMenuItem disabled_lang in languageToolStripMenuItem.DropDownItems){
                disabled_lang.Checked = false;
            }
        }
        private void LanguageToolStripMenuItem_Click(object sender, EventArgs e){
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string langCode){
                if (lang != langCode && AllLanguageFiles.ContainsKey(langCode)){
                    Lang_preload(AllLanguageFiles[langCode], langCode);
                    Select_lang_active(sender);
                }
            }
        }
        private void Lang_preload(string lang_type, string lang_code){
            Lang_engine(lang_type, lang_code);
            try{
                TSSettingsModule software_setting_save = new TSSettingsModule(ts_sf);
                software_setting_save.TSWriteSettings(ts_settings_container, "LanguageStatus", lang_code);
            }catch (Exception){ }
            // LANG CHANGE NOTIFICATION
            // TSGetLangs software_lang = new TSGetLangs(lang_path);
            // DialogResult lang_change_message = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("LangChange", "lang_change_notification"), "\n\n", "\n\n"));
            // if (lang_change_message == DialogResult.Yes) { Application.Restart(); }
        }
        private void Lang_engine(string lang_type, string lang_code){
            try{
                lang_path = lang_type;
                lang = lang_code;
                // GLOBAL ENGINE
                TSGetLangs software_lang = new TSGetLangs(lang_path);
                // FILE
                fileToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_file");
                importFileToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderFileContent", "header_fc_import");
                exportFileToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderFileContent", "header_fc_export");
                // VCARD VERSION
                vCardVersionToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_vcard_version");
                vcard21ToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderVCardVersion", "header_vcard_21");
                vcard30ToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderVCardVersion", "header_vcard_30");
                vcard40ToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderVCardVersion", "header_vcard_40");
                // SORTING MODE
                sortingModeToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_sorting");
                sortingFullNameToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderSortingMode", "header_sm_full");
                sortingFirstNameToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderSortingMode", "header_sm_firstname");
                sortingLastNameToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderSortingMode", "header_sm_lastname");
                sortingPhoneToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderSortingMode", "header_sm_phonemobile");
                // SETTINGS
                settingsToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_settings");
                // THEMES
                themeToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_theme");
                lightThemeToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderThemes", "theme_light");
                darkThemeToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderThemes", "theme_dark");
                systemThemeToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderThemes", "theme_system");
                // LANGS
                languageToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_language");
                arabicToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_ar");
                chineseToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_zh");
                englishToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_en");
                dutchToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_nl");
                frenchToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_fr");
                germanToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_de");
                hindiToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_hi");
                italianToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_it");
                japaneseToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_ja");
                koreanToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_ko");
                polishToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_pl");
                portugueseToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_pt");
                russianToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_ru");
                spanishToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_es");
                turkishToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderLangs", "lang_tr");
                // STARTUP MODE
                startupToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_start");
                windowedToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderViewMode", "header_view_mode_windowed");
                fullScreenToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderViewMode", "header_view_mode_full_screen");
                // UPDATE
                checkForUpdateToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_update");
                // DONATE
                donateToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_donate");
                // ABOUT
                aboutToolStripMenuItem.Text = software_lang.TSReadLangs("HeaderMenu", "header_menu_about");
                // CX
                cxImageSelectToolStripMenuItem.Text = software_lang.TSReadLangs("CxImageMenu", "cxim_select");
                cxViewImageToolStripMenuItem.Text = software_lang.TSReadLangs("CxImageMenu", "cxim_view");
                cxSaveImageToolStripMenuItem.Text = software_lang.TSReadLangs("CxImageMenu", "cxim_save");
                cxRemoveImageToolStripMenuItem.Text = software_lang.TSReadLangs("CxImageMenu", "cxim_remove");
                // UI
                lblSearch.Text = software_lang.TSReadLangs("VCardixUI", "vcui_search");
                BtnAdd.Text = " " + software_lang.TSReadLangs("VCardixUI", "vcui_add");
                BtnUpdate.Text = " " + software_lang.TSReadLangs("VCardixUI", "vcui_update");
                BtnDelete.Text = " " + software_lang.TSReadLangs("VCardixUI", "vcui_delete");
                // UI FORM
                lblFirstName.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_name");
                lblMiddleName.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_sec_name");
                lblLastName.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_surname");
                lblBirthday.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_birthday");
                lblPhoneCell.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_phone_cell");
                lblPhoneHome.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_phone_home");
                lblPhoneWork.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_phone_work");
                lblMail1.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_email_1");
                lblMail2.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_email_2");
                lblMail3.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_email_3");
                lblAdress.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_address");
                lblOrganization.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_organization");
                lblWebsite.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_website");
                lblNote.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_note");
                lblProfileImage.Text = software_lang.TSReadLangs("VCardixUI", "vcui_f_profile_image");
                //
                if (ContactList.Items.Count == 0){
                    BottomInfoLabel.Text = software_lang.TSReadLangs("VCardixUI", "vcui_bottom_ready_empty");
                }else{
                    BottomInfoLabel.Text = string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_bottom_ready"), ContactList.Items.Count.ToString());
                }
                //
                MainToolTip.RemoveAll();
                MainToolTip.SetToolTip(textBoxSearch, software_lang.TSReadLangs("VCardixUI", "vcui_search_hover"));
                MainToolTip.SetToolTip(BtnOpenAdressWindow, software_lang.TSReadLangs("VCardixUI", "vcui_adress_window_hover"));
                MainToolTip.SetToolTip(ContactUserImage, software_lang.TSReadLangs("VCardixUI", "vcui_profile_image_hover"));
                //
                Software_other_page_preloader();
            }catch (Exception){ }
        }
        // STARTUP SETINGS
        // ======================================================================================================
        private void Select_startup_mode_active(object target_startup_mode){
            ToolStripMenuItem selected_startup_mode = null;
            Select_startup_mode_deactive();
            if (target_startup_mode != null){
                if (selected_startup_mode != (ToolStripMenuItem)target_startup_mode){
                    selected_startup_mode = (ToolStripMenuItem)target_startup_mode;
                    selected_startup_mode.Checked = true;
                }
            }
        }
        private void Select_startup_mode_deactive(){
            foreach (ToolStripMenuItem disabled_startup in startupToolStripMenuItem.DropDownItems){
                disabled_startup.Checked = false;
            }
        }
        private void WindowedToolStripMenuItem_Click(object sender, EventArgs e){
            if (startup_status != 0){ startup_status = 0; Startup_mode_settings("0"); Select_startup_mode_active(sender); }
        }
        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e){
            if (startup_status != 1){ startup_status = 1; Startup_mode_settings("1"); Select_startup_mode_active(sender); }
        }
        private void Startup_mode_settings(string get_startup_value){
            try{
                TSSettingsModule software_setting_save = new TSSettingsModule(ts_sf);
                software_setting_save.TSWriteSettings(ts_settings_container, "StartupStatus", get_startup_value);
            }catch (Exception){ }
        }
        // UPDATE CHECK ENGINE
        // ======================================================================================================
        private void CheckForUpdateToolStripMenuItem_Click(object sender, EventArgs e){
            Task.Run(() => Software_update_check(1));
        }
        public async void Software_update_check(int _check_update_ui){
            try{
                TSGetLangs software_lang = new TSGetLangs(lang_path);
                SetUpdateMenuEnabled(false);
                if (!await IsNetworkAvailable()){
                    if (_check_update_ui == 1){
                        TS_MessageBoxEngine.TS_MessageBox(this, 2, string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_not_connection"), "\n\n"), string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_title"), Application.ProductName));
                    }
                    return;
                }
                using (HttpClientHandler handler = new HttpClientHandler()){
                    handler.UseProxy = false;
                    using (HttpClient httpClient = new HttpClient(handler)){
                        httpClient.Timeout = TimeSpan.FromSeconds(15);
                        httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue{ NoCache = true, NoStore = true, MustRevalidate = true };
                        httpClient.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
                        string versionUrl = TS_LinkSystem.github_link_lv;
                        versionUrl += (versionUrl.Contains("?") ? "&" : "?") + "_ts=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        string response = await httpClient.GetStringAsync(versionUrl);
                        string firstLine = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
                        string client_version_raw = TS_VersionParser.ParseUINormalize(Application.ProductVersion);
                        string last_version_raw = TS_VersionParser.ParseUINormalize(firstLine.Split(new[] { '=' }, 2)[1].Trim());
                        Version client_ver = Version.Parse(client_version_raw);
                        Version last_ver = Version.Parse(last_version_raw);
                        if (client_ver < last_ver){
                            DialogResult info_update = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_available"), Application.ProductName, "\n\n", client_version_raw, "\n", last_version_raw, "\n\n"), string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_title"), Application.ProductName));
                            if (info_update == DialogResult.Yes){
                                try{
                                    string updaterPath = Path.Combine(Application.StartupPath, Program.updater_exe_name);
                                    if (File.Exists(updaterPath)){
                                        string procName = Path.GetFileNameWithoutExtension(updaterPath);
                                        bool isRunning = Process.GetProcessesByName(procName).Length > 0;
                                        if (!isRunning){
                                            Process.Start(new ProcessStartInfo(updaterPath) { UseShellExecute = true, Arguments = $"-app={Application.ProductName}" });
                                        }else{
                                            TS_MessageBoxEngine.TS_MessageBox(this, 1, software_lang.TSReadLangs("SoftwareUpdate", "su_ts_updater_c_running"), string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_title"), Application.ProductName));
                                        }
                                        Application.Exit();
                                        return;
                                    }else{
                                        TS_MessageBoxEngine.TS_MessageBox(this, 2, string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_ts_updater_not_available"), Program.updater_exe_name), string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_title"), Application.ProductName));
                                        Process.Start(new ProcessStartInfo(TS_LinkSystem.github_link_lr) { UseShellExecute = true });
                                        Application.Exit();
                                        return;
                                    }
                                }catch (Exception ex){
                                    Debug.WriteLine(ex, $"{Program.updater_exe_name} launch block.");
                                }
                            }
                        }else if (_check_update_ui == 1){
                            string update_msg = client_ver == last_ver ? string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_not_available"), Application.ProductName, "\n", client_version_raw) : string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_newer"), "\n\n", $"v{client_version_raw}");
                            TS_MessageBoxEngine.TS_MessageBox(this, 1, update_msg, string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_title"), Application.ProductName));
                        }
                    }
                }
            }catch (Exception ex){
                Debug.WriteLine(ex, "Software_update_check()");
                TSGetLangs software_lang = new TSGetLangs(lang_path);
                TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_error"), "\n\n", ex.Message), string.Format(software_lang.TSReadLangs("SoftwareUpdate", "su_title"), Application.ProductName));
            }finally{
                SetUpdateMenuEnabled(true);
            }
        }
        private void SetUpdateMenuEnabled(bool enabled){
            if (InvokeRequired){
                BeginInvoke(new Action(() => checkForUpdateToolStripMenuItem.Enabled = enabled));
            }else{
                checkForUpdateToolStripMenuItem.Enabled = enabled;
            }
        }
        // TS TOOL LAUNCHER MODULE
        // ======================================================================================================
        private void TSToolLauncher<T>(string formName, string langKey, bool mode) where T : Form, new(){
            try{
                TSGetLangs software_lang = new TSGetLangs(lang_path);
                T tool = new T { Name = formName };
                if (Application.OpenForms[formName] == null){
                    if (mode) tool.ShowDialog(); else tool.Show();
                }
                else{
                    if (Application.OpenForms[formName].WindowState == FormWindowState.Minimized){
                        Application.OpenForms[formName].WindowState = FormWindowState.Normal;
                    }
                    string public_message;
                    public_message = string.Format(software_lang.TSReadLangs("HeaderHelp", "header_help_info_notification"), software_lang.TSReadLangs("VCardixOther", langKey));
                    if (formName == "vcardix_about"){
                        public_message = string.Format(software_lang.TSReadLangs("HeaderHelp", "header_help_info_notification"), software_lang.TSReadLangs("HeaderMenu", langKey));
                    }
                    TS_MessageBoxEngine.TS_MessageBox(this, 1, public_message);
                    Application.OpenForms[formName].Activate();
                }
            }catch (Exception){ }
        }
        // OPEN ADDRESS WINDOW
        // ======================================================================================================
        private void BtnOpenAdressWindow_Click(object sender, EventArgs e){
            TSToolLauncher<VCardixAdressWindow>("vcardix_adress_window", "vca_name", true);
        }
        private void TextBoxAddress_KeyPress(object sender, KeyPressEventArgs e){
            TSToolLauncher<VCardixAdressWindow>("vcardix_adress_window", "vca_name", true);
            e.Handled = true;
        }
        // IMAGE PREVIEW WINDOW
        private void CxViewImageToolStripMenuItem_Click(object sender, EventArgs e){
            TSToolLauncher<VCardixImagePreview>("vcardix_image_preview", "vci_name", true);
        }
        // DONATE LINK
        // ======================================================================================================
        private void DonateToolStripMenuItem_Click(object sender, EventArgs e){
            try{
                Process.Start(new ProcessStartInfo(TS_LinkSystem.ts_donate){ UseShellExecute = true });
            }catch (Exception){ }
        }
        // ABOUT
        // ======================================================================================================
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e){
            TSToolLauncher<VCardixAbout>("vcardix_about", "header_menu_about", false);
        }
        // EXIT
        // ======================================================================================================
        private void VCardix_FormClosing(object sender, FormClosingEventArgs e){
            if (!save_status){
                TSGetLangs software_lang = new TSGetLangs(lang_path);
                var import_warning = TS_MessageBoxEngine.TS_MessageBox(this, 10, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_import_warning_exit"), "\n\n"));
                if (import_warning == DialogResult.Yes){
                    try{
                        SaveContactsToFile();
                        Application.Exit();
                    }catch (Exception ex){
                        TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("VCardixUI", "vcui_export_failed"), "\n\n", ex.Message));
                        e.Cancel = true;
                        return;
                    }
                }else if (import_warning == DialogResult.No){
                    Application.ExitThread();
                }else if (import_warning == DialogResult.Cancel){
                    e.Cancel = true;
                    return;
                }
            }else{
                Application.Exit();
            }
        }
    }
}