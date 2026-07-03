using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
//
using static VCardix.TSModules;
using static VCardix.VCardixModule;

namespace VCardix{
    public partial class VCardixImagePreview : Form{
        public VCardixImagePreview(){ InitializeComponent(); }
        // LOAD
        // ======================================================================================================
        private async void VCardixImagePreview_Load(object sender, EventArgs e){
            Image_preview_preloader();
            try { await Task.Run(() => { AsyncLoadImage(); }); } catch (Exception) { }
        }
        private void AsyncLoadImage(){
            if (VCardixMain.select_object is PrefixModule c){
                TSImageHelper.SetPictureBoxImage(ImgPreview, c.PhotoImage);
            }
        }
        // DYNAMIC UI
        // ======================================================================================================
        public void Image_preview_preloader(){
            try{
                TSThemeModeHelper.InitializeThemeForForm(this);
                //
                BackColor = TS_ThemeEngine.ColorMode(VCardixMain.theme, "TSBT_BGColor2");
                //
                foreach (Control ui_controls in BackPanel.Controls){
                    if (ui_controls is Button ui_btn){
                        ui_btn.ForeColor = TS_ThemeEngine.ColorMode(VCardixMain.theme, "TSBT_BGColor2");
                        ui_btn.BackColor = TS_ThemeEngine.ColorMode(VCardixMain.theme, "TSBT_AccentColor");
                        ui_btn.FlatAppearance.BorderColor = TS_ThemeEngine.ColorMode(VCardixMain.theme, "TSBT_AccentColor");
                        ui_btn.FlatAppearance.MouseDownBackColor = TS_ThemeEngine.ColorMode(VCardixMain.theme, "TSBT_AccentColor");
                        ui_btn.FlatAppearance.MouseOverBackColor = TS_ThemeEngine.ColorMode(VCardixMain.theme, "AccentColorHover");
                    }
                }
                //
                TSImageRenderer(BtnSaveImg, VCardixMain.theme == 1 ? Properties.Resources.ct_save_image_mc_light : Properties.Resources.ct_save_image_mc_dark, 16, ContentAlignment.MiddleRight);
                // ======================================================================================================
                // TEXTS
                TSGetLangs software_lang = new TSGetLangs(VCardixMain.lang_path);
                Text = string.Format(software_lang.TSReadLangs("VCardixOther", "vci_title"), Application.ProductName);
                BtnSaveImg.Text = " " + software_lang.TSReadLangs("VCardixOther", "vci_img_save");
            }catch (Exception){ }
        }
        // SAVE IMAGE
        // ======================================================================================================
        private void BtnSaveImg_Click(object sender, EventArgs e){
            if (VCardixMain.select_object is PrefixModule c){
                VCardixMain.SaveContentImage(TSImageHelper.ImageFromBase64(c.PhotoBase64), this);
            }
        }
        // DISPOSE AND EXIT
        // ======================================================================================================
        private void VCardixImagePreview_FormClosing(object sender, FormClosingEventArgs e){
            TSImageHelper.SetPictureBoxImage(ImgPreview, null);
        }
    }
}