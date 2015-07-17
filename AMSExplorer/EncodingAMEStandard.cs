﻿//----------------------------------------------------------------------------------------------
//    Copyright 2015 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//--------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Reflection;
using System.Diagnostics;

namespace AMSExplorer
{
    public partial class EncodingAMEStandard : Form
    {
        public string EncodingAMEStdPresetXMLFilesUserFolder;
        public string EncodingAMEStdPresetXMLFilesFolder;

        private SubClipConfiguration _subclipConfig;

        private List<IMediaProcessor> Procs;
        public List<IAsset> SelectedAssets;
        private CloudMediaContext _context;

        private const string defaultprofile = "H264 Multiple Bitrate 720p";
        bool usereditmode = false;

        public string EncodingLabel
        {
            set
            {
                label.Text = value;
            }
        }

        public string EncodingJobName
        {
            get
            {
                return textBoxJobName.Text;
            }
            set
            {
                textBoxJobName.Text = value;
            }
        }


        public List<IMediaProcessor> EncodingProcessorsList
        {
            set
            {
                foreach (IMediaProcessor pr in value)
                {
                    comboBoxProcessor.Items.Add(string.Format("{0} {1} Version {2} ({3})", pr.Vendor, pr.Name, pr.Version, pr.Description));
                }
                if (comboBoxProcessor.Items.Count > 0)
                {
                    comboBoxProcessor.SelectedIndex = 0;
                }
                Procs = value;
            }
        }

        public IMediaProcessor EncodingProcessorSelected
        {
            get
            {
                return Procs[comboBoxProcessor.SelectedIndex];
            }
        }

        public string EncodingOutputAssetName
        {
            get
            {
                return textboxoutputassetname.Text;
            }
            set
            {
                textboxoutputassetname.Text = value;
            }
        }


        public string EncodingConfiguration
        {
            get
            {
                return textBoxConfiguration.Text;
            }
        }

        public JobOptionsVar JobOptions
        {
            get
            {
                return buttonJobOptions.GetSettings();
            }
            set
            {
                buttonJobOptions.SetSettings(value);
            }
        }


        public EncodingAMEStandard(CloudMediaContext context, SubClipConfiguration subclipConfig = null)
        {
            InitializeComponent();
            this.Icon = Bitmaps.Azure_Explorer_ico;
            _context = context;
            _subclipConfig = subclipConfig; // used for trimming
            buttonJobOptions.Initialize(_context);
        }



        private void EncodingAMEStandard_Shown(object sender, EventArgs e)
        {
        }

        private void EncodingAMEStandard_Load(object sender, EventArgs e)
        {
            // presets list
            var filePaths = Directory.GetFiles(EncodingAMEStdPresetXMLFilesFolder, "*.xml").Select(f => Path.GetFileNameWithoutExtension(f));
            listboxPresets.Items.AddRange(filePaths.ToArray());
            listboxPresets.SelectedIndex = listboxPresets.Items.IndexOf(defaultprofile);
            label4KWarning.Text = string.Empty;
            moreinfoame.Links.Add(new LinkLabel.Link(0, moreinfoame.Text.Length, Constants.LinkMoreInfoMES));

        }


        private void buttonLoadXML_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(this.EncodingAMEStdPresetXMLFilesUserFolder))
                openFileDialogPreset.InitialDirectory = this.EncodingAMEStdPresetXMLFilesUserFolder;

            if (openFileDialogPreset.ShowDialog() == DialogResult.OK)
            {
                this.EncodingAMEStdPresetXMLFilesUserFolder = Path.GetDirectoryName(openFileDialogPreset.FileName); // let's save the folder
                try
                {
                    StreamReader streamReader = new StreamReader(openFileDialogPreset.FileName);
                    UpdateTextBoxXML(streamReader.ReadToEnd());
                    //textBoxConfiguration.Text = streamReader.ReadToEnd();
                    streamReader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }

                label4KWarning.Text = string.Empty;
                buttonOk.Enabled = true;
            }
        }

        private void UpdateTextBoxXML(string xmldata)
        {
            if (_subclipConfig == null || !_subclipConfig.Trimming)
            {
                textBoxConfiguration.Text = xmldata;
            }
            else
            {
                // Update the xml with trimming
                XDocument doc = XDocument.Parse(xmldata);
                XNamespace ns = "http://www.windowsazure.com/media/encoding/Preset/2014/03";

                var presetxml = doc.Element(ns + "Preset");
                var encodingxml = presetxml.Element(ns + "Encoding");

                if (presetxml != null)
                {
                    if (presetxml.Element(ns + "Sources") == null)
                    {
                        if (encodingxml != null)
                        {
                            encodingxml.AddBeforeSelf(new XElement(ns + "Sources", new XElement(ns + "Source"))); // order is important !
                        }
                        else
                        {
                            presetxml.AddFirst(new XElement(ns + "Sources", new XElement(ns + "Source")));
                        }
                    }
                    var sourcesxml = presetxml.Element(ns + "Sources");
                    if (sourcesxml.Element(ns + "Source") == null)
                    {
                        sourcesxml.Add(new XElement(ns + "Source"));
                    }
                    var sourcexml = sourcesxml.Element(ns + "Source");
                    sourcexml.SetAttributeValue("StartTime", _subclipConfig.StartTimeForReencode);
                    sourcexml.SetAttributeValue("Duration", _subclipConfig.DurationForReencode);
                }
                textBoxConfiguration.Text = doc.Declaration.ToString() + doc.ToString();
            }
        }

        private void buttonSaveXML_Click(object sender, EventArgs e)
        {
            if (saveFileDialogPreset.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialogPreset.FileName, textBoxConfiguration.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not save file to disk. Original error: " + ex.Message);
                }

            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void listboxPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listboxPresets.SelectedItem != null)
            {
                try
                {
                    string filePath = Path.Combine(EncodingAMEStdPresetXMLFilesFolder, listboxPresets.SelectedItem.ToString() + ".xml");
                    StreamReader streamReader = new StreamReader(filePath);
                    usereditmode = false;
                    UpdateTextBoxXML(streamReader.ReadToEnd());
                    //textBoxConfiguration.Text = streamReader.ReadToEnd();
                    usereditmode = true;
                    streamReader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                    usereditmode = true;
                }

                if (listboxPresets.SelectedItem.ToString().Contains("4K") && _context.EncodingReservedUnits.FirstOrDefault().ReservedUnitType!=ReservedUnitType.Premium)
                {
                    label4KWarning.Text = (string)label4KWarning.Tag;
                    buttonOk.Enabled = false;
                }
                else
                {
                    label4KWarning.Text = string.Empty;
                    buttonOk.Enabled = true;
                }

            }
        }

        private void textBoxConfiguration_TextChanged(object sender, EventArgs e)
        {
            if (usereditmode) listboxPresets.SelectedIndex = -1;
        }

        private void moreinfoame_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string);

        }
    }



}
