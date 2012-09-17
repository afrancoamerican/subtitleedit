﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using Nikse.SubtitleEdit.Logic;

namespace Nikse.SubtitleEdit.Forms
{
    public partial class PluginsGet : Form
    {
        private XmlDocument _pluginDoc = new XmlDocument();
        private string _downloadedPluginName;

        public PluginsGet()
        {
            InitializeComponent();

            try
            {
                labelPleaseWait.Text = Configuration.Settings.Language.General.PleaseWait;
                this.Refresh();
                string url = "http://www.nikse.dk/Content/SubtitleEdit/Plugins/Index.xml";
                var wc = new WebClient { Proxy = Utilities.GetProxy() };
                wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(PluginListDownloadDataCompleted);
                wc.DownloadDataAsync(new Uri(url));
            }
            catch (Exception exception)
            {
                labelPleaseWait.Text = string.Empty;
                buttonOK.Enabled = true;
                buttonDownload.Enabled = true;
                listViewPlugins.Enabled = true;
                MessageBox.Show(exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace);
            }
        }

        void PluginListDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("Download failed: " + e.Error.Message);
                DialogResult = DialogResult.Cancel;
                return;
            }
            try
            {
                _pluginDoc.LoadXml(System.Text.Encoding.UTF8.GetString(e.Result));
                foreach (XmlNode node in _pluginDoc.DocumentElement.SelectNodes("Plugin"))
                {
                    ListViewItem item = new ListViewItem(node.SelectSingleNode("Name").InnerText);
                    item.SubItems.Add(node.SelectSingleNode("Description").InnerText);
                    item.SubItems.Add(node.SelectSingleNode("Version").InnerText);
                    item.SubItems.Add(node.SelectSingleNode("Date").InnerText);
                    listViewPlugins.Items.Add(item);
                }
            }
            catch
            {
                MessageBox.Show("Load of downloaded xml plugin-list faild!");
            }
        }


        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (listViewPlugins.SelectedItems.Count == 0)
                return;

            try
            {
                labelPleaseWait.Text = Configuration.Settings.Language.General.PleaseWait;
                buttonOK.Enabled = false;
                buttonDownload.Enabled = false;
                listViewPlugins.Enabled = false;
                this.Refresh();
                Cursor = Cursors.WaitCursor;

                int index = listViewPlugins.SelectedItems[0].Index;
                string url = _pluginDoc.DocumentElement.SelectNodes("Plugin")[index].SelectSingleNode("Url").InnerText;
                _downloadedPluginName = _pluginDoc.DocumentElement.SelectNodes("Plugin")[index].SelectSingleNode("Name").InnerText;

                var wc = new WebClient { Proxy = Utilities.GetProxy() };
                wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(wc_DownloadDataCompleted);
                wc.DownloadDataAsync(new Uri(url));
                Cursor = Cursors.Default;
            }
            catch (Exception exception)
            {
                labelPleaseWait.Text = string.Empty;
                buttonOK.Enabled = true;
                buttonDownload.Enabled = true;
                listViewPlugins.Enabled = true;
                Cursor = Cursors.Default;
                MessageBox.Show(exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace);
            }
        }

        void wc_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("Download failed!");
                DialogResult = DialogResult.Cancel;
                return;
            }

            string pluginsFolder = Path.Combine(Configuration.DataDirectory, "Plugins");
            if (!Directory.Exists(pluginsFolder))
                Directory.CreateDirectory(pluginsFolder);

            var ms = new MemoryStream(e.Result);

            ZipExtractor zip = ZipExtractor.Open(ms);
            List<ZipExtractor.ZipFileEntry> dir = zip.ReadCentralDir();

            // Extract dic/aff files in dictionary folder
            foreach (ZipExtractor.ZipFileEntry entry in dir)
            {
                string fileName = Path.GetFileName(entry.FilenameInZip);
                string fullPath = Path.Combine(pluginsFolder, fileName);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch
                    {
                        MessageBox.Show(string.Format("{0} already exists - unable to overwrite it", fullPath));
                        Cursor = Cursors.Default;
                        labelPleaseWait.Text = string.Empty;
                        buttonOK.Enabled = true;
                        buttonDownload.Enabled = true;
                        listViewPlugins.Enabled = true;
                        return;
                    }
                }
                zip.ExtractFile(entry, fullPath);
            }
            zip.Close();
            ms.Close();
            Cursor = Cursors.Default;
            labelPleaseWait.Text = string.Empty;
            buttonOK.Enabled = true;
            buttonDownload.Enabled = true;
            listViewPlugins.Enabled = true;
            MessageBox.Show(string.Format("Plugin '{0}' downloaded", _downloadedPluginName));
        }

        private void linkLabelOpenDictionaryFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string pluginsFolder = Path.Combine(Configuration.DataDirectory, "Plugins");
            if (!Directory.Exists(pluginsFolder))
                Directory.CreateDirectory(pluginsFolder);

            System.Diagnostics.Process.Start(pluginsFolder);
        }

        private void PluginsGet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                DialogResult = DialogResult.Cancel;
        }


    }
}
