using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using EasyModbus;
using System.Collections;

namespace PSU_B
{
    public partial class MainForm : Form
    {
        
        UInt16[] bTx, bRx, Regs;

        
        // masks for commands
        enum Masks
        {
            Mode = 0x0F800000
        };

        enum Mode
        {
            Bit_Manual  = 0x00800000,
            HF_Active   = 0x01000000,
            CL_Mon      = 0x02000000,
            Int_Test    = 0x04000000,
            Ext_Test    = 0x08000000
        };

        enum AMP
        {
            BYPASS1     = 0x20000000
        };

        enum LED
        {
            PowerGood  = 0x1,
            DcPower     = 0x2,
            Local       = 0x4,
            Remote      = 0x8,
            Hf_Active   = 0x10,
            Cl_Mon      = 0x20,
            Ext_Test    = 0x40,
            Int_Test    = 0x80,
            Amp1_Bypass = 0x100,
            
        };

        enum State: uint
        {
            Reg_LIMITER_CONT        = 0x00000020,
            Reg_1ST_AMP_CONT        = 0x00000040, 
            Reg_1_30MHz_S_V1        = 0x00010000,
            Reg_1_30MHz_S_V2        = 0x00020000,
            Reg_1st_AMP_BYPASS      = 0x00040000,
            Reg_COMB_G_ON_OFF_POS   = 0x00080000,
            Reg_BIT_CONT_1_COMB     = 0x00100000,
            Reg_BIT_CONT_2_COMB     = 0x00200000,
            BIT                     = 0x10000000,
        }

        enum BitSouceEnum
        {
            HF_Active,
            CL_Mon,
            Int_Test,
            Ext_Test
        }

        float originalWidth;
        float originalHeight;
        private Dictionary<Control, Size> defaultSizes = new Dictionary<Control, Size>();
        private Dictionary<Control, Font> defaultFonts = new Dictionary<Control, Font>();
        private Dictionary<Control, Point> defaultLocations = new Dictionary<Control, Point>();

        public MainForm()
        {
            InitializeComponent();
            // disable communication buttons
            OpRW_Buttons(false);
            // Subscribe to the Resize event of the form
            this.Resize += Form1_Resize;
            // Store default sizes and fonts when the form is first loaded
            StoreDefaultSizesAndFonts(this.Controls);
            InitButtons();
            bTx = new UInt16[50];
            bRx = new UInt16[50];
            Regs = new UInt16[256];
            // manually invoke the text change events to update the Registers.
            textBoxSetAddress_TextChanged(textBoxSetAddress, EventArgs.Empty);
            textBoxSetSubaddress_TextChanged(textBoxSetSubaddress, EventArgs.Empty);
            textBoxSetGateway_TextChanged(textBoxSetGateway, EventArgs.Empty);

            // Attach the empty event handler to each radio button // used to change property to radio button instead
            AddCheckedChangedHandler(this);

            originalWidth = this.Width;
            originalHeight = this.Height;
        }
        private void OpRW_Buttons(bool on)
        {
            if (on)
            {
                bReadAux.Enabled = true;
                bReadAuxEeprom.Enabled = true;
                bRead9408Y.Enabled = true;
                bRead9408Discrete.Enabled = true;
                bRead9800Y.Enabled = true;
                bRead9800Dis.Enabled = true;
                bReadMaint.Enabled = true;
                bSetAddress.Enabled = true;
                bResetCpu.Enabled = true;
                bWrite6CTo6E.Enabled = true;
                bWrite6CTo6E.Enabled = true;
                bWriteAuxEeprom.Enabled = true;
                bWrite9408Y.Enabled = true;
                bWrite9408Discrete.Enabled = true;
                bWrite9800Y.Enabled = true;
                bWrite9800Dis.Enabled = true;
                bWriteMaint.Enabled = true;
            }
            else
            {
                bReadAux.Enabled = false;
                bReadAuxEeprom.Enabled = false;
                bRead9408Y.Enabled = false;
                bRead9408Discrete.Enabled = false;
                bRead9800Y.Enabled = false;
                bRead9800Dis.Enabled = false;
                bReadMaint.Enabled = false;
                bSetAddress.Enabled = false;
                bResetCpu.Enabled = false;
                bWrite6CTo6E.Enabled = false;
                bWrite6CTo6E.Enabled = false;
                bWriteAuxEeprom.Enabled = false;
                bWrite9408Y.Enabled = false;
                bWrite9408Discrete.Enabled = false;
                bWrite9800Y.Enabled = false;
                bWrite9800Dis.Enabled = false;
                bWriteMaint.Enabled = false;
            }
        }

        // Method to add a checked changed event handler to all radio buttons in a form
        private void AddCheckedChangedHandler(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is RadioButton radioButton)
                {
                    //radioButton.Click += RadioButton_Click;
                    radioButton.AutoCheck = false;
                }
                else if (control.HasChildren)
                {
                    // Recursively call the method for nested controls
                    AddCheckedChangedHandler(control);
                }
            }
        }

        // The event handler that does nothing
        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            // No action is taken here
            Console.WriteLine("Change Event");
        }

        private void RadioButton_Click(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            // Toggle the checked state manually
            radioButton.Checked = !radioButton.Checked;
        }

        private void StoreDefaultSizesAndFonts(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                // Store default size and font
                defaultSizes[control] = control.Size;
                defaultFonts[control] = control.Font;
                defaultLocations[control] = control.Location;

                // Recursively store default sizes and fonts for child controls
                if (control.Controls.Count > 0)
                {
                    StoreDefaultSizesAndFonts(control.Controls);
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                // Calculate scaling factor
                float scaleFactorWidth = (float)this.ClientSize.Width / originalWidth; // Assuming original width is 800
                float scaleFactorHeight = (float)this.ClientSize.Height / originalHeight; // Assuming original height is 600

                // Resize text font proportionally
                float scaleFactor = Math.Min(scaleFactorWidth, scaleFactorHeight);
                AdjustControlSizesAndFonts(this.Controls, scaleFactorWidth, scaleFactorHeight);
            }
            if (this.WindowState == FormWindowState.Normal)
            {
                // Form returned to its normal size
                // Reset control sizes and fonts to default
                ResetControlSizesAndFonts(this.Controls);
            }
        }

        private void ResetControlSizesAndFonts(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                // Reset size and font to default
                control.Size = defaultSizes[control];
                control.Font = defaultFonts[control];
                control.Location = defaultLocations[control];

                // Recursively reset sizes and fonts for child controls
                if (control.Controls.Count > 0)
                {
                    ResetControlSizesAndFonts(control.Controls);
                }
            }
        }

        private void AdjustControlSizesAndFonts(Control.ControlCollection controls, float scaleFactorWidth, float scaleFactorHeight)
        {
            foreach (Control control in controls)
            {
                // Adjust font size for Label, Button, TextBox, etc.
                control.Font = new Font(control.Font.FontFamily, 14, control.Font.Style);

                // Adjust control size
                control.Width = (int)(control.Width * scaleFactorWidth);
                control.Height = (int)(control.Height * scaleFactorHeight);

                // Adjust control location
                control.Left = (int)(control.Left * scaleFactorWidth);
                control.Top = (int)(control.Top * scaleFactorHeight);

                // Recursively adjust fonts and sizes for child controls
                if (control.Controls.Count > 0)
                {
                    AdjustControlSizesAndFonts(control.Controls, scaleFactorWidth, scaleFactorHeight);
                }
            }
        }

        TcpClient socketForServer = null;
        NetworkStream netWorkStream = null;
        private void TcpClosePort()
        {
            try 
            {
                socketForServer.Close();
                socketForServer = null;
                netWorkStream.Close();
                netWorkStream = null;
            }
            catch 
            {

            }
            
        }
        ModbusClient modbusClient;
        private int TcpOpenPort()
        {
            //if (socketForServer == null)
            try
            {
                string address = tIpAddress.Text;
                int port = 502;

                // Modbus TCP client setup
                modbusClient = new ModbusClient(address, port);
            //lanTest = 1;
            }
            catch
            {
                Console.WriteLine("Failed to Connect to Server");
                //lanTest = 0;
                //UpdateRadioButtons();
                return -1;
            }
            return 0;

        }
        int sendCount = 0, receiveCount = 0;

        private void ReadRegs(int start, int num)
        {
            try
            {
                // Read holding registers
                sendCount++;
                int[] readData = modbusClient.ReadHoldingRegisters(start, num);
                receiveCount++;
                //Array.Copy(readData, 0, Regs, start, num);
                // copy and convert type
                for (int i = 0; i < readData.Length; i++)
                {
                    Regs[i + start] = (UInt16)readData[i];
                }
                // Display the read data
                Console.WriteLine("Read Data:");
                for (int i = 0; i < readData.Length; i++)
                {
                    Console.WriteLine($"Register {start + i}: {readData[i]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
        }

        private void WriteReg(int address, int data)
        {
            try
            {
                // Read holding registers
                sendCount++;
                modbusClient.WriteSingleRegister(address, data);
                receiveCount++;

                // Display the read data
                Console.WriteLine("Write Data:");
                Console.WriteLine($"Register {address}: {data}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }

        private void WriteMulRegs(int address, int[] data)
        {
            try
            {
                // Read holding registers
                sendCount++;
                modbusClient.WriteMultipleRegisters(address, data);
                receiveCount++;
                // Display the write data
                Console.WriteLine("Write Data:");
                Console.Write($"Register {address}: ");
                for (int i = 0; i < data.Length; i++)
                {
                    Console.Write($"{data[i]} ");
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }




        private void tIpAddress_TextChanged(object sender, EventArgs e)
        {
            //byte[] buf = IPAddress.Parse(tIpAddress.Text).GetAddressBytes();
            //Debug.WriteLine("{0}, {1}, {2}, {3}",buf[0],buf[1],buf[2], buf[3] );
            //IPAddress myip = new IPAddress(buf);
            //Debug.WriteLine(myip.ToString());
            //TcpClosePort();
        }

        private void bOpenPort_Click(object sender, EventArgs e)
        {
            if (TcpOpenPort() >= 0)
            {
                //bRead.Enabled = true;
                //bWrite.Enabled = true;
                bConnect.Enabled = true;
            }
            else
            {
                
                bRead.Enabled = false;
                bWrite.Enabled = false;
                bWriteMul.Enabled = false;
                // disable communication buttons
                OpRW_Buttons(false);
            }
        }

        private void bRead_Click(object sender, EventArgs e)
        {
            ReadRegs((int)nRegsAddressRead.Value, (int)nRegsNum.Value);
            UpdateGui();
        }

        private void InitButtons()
        {
            bRead.Enabled = false;
            bWrite.Enabled = false;
            bWriteMul.Enabled = false;
            bConnect.Enabled = false;
        }

        private void bConnect_Click(object sender, EventArgs e)
        {
            if (bConnect.Text == "Connect")
                try
                {
                    // Connect to the Modbus TCP server
                    modbusClient.Connect();
                    bRead.Enabled = true;
                    bWrite.Enabled = true;
                    bWriteMul.Enabled = true;
                    bConnect.Text = "Disconnect";
                    // enable communication buttons
                    OpRW_Buttons(true);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    bRead.Enabled = false;
                    bWrite.Enabled = false;
                    bWriteMul.Enabled = false;
                    modbusClient.Disconnect();
                    // disable communication buttons
                    OpRW_Buttons(false);
                }
            else
            {
                bConnect.Text = "Connect";
                bRead.Enabled = false;
                bWrite.Enabled = false;
                bWriteMul.Enabled = false;
                modbusClient.Disconnect();
                // disable communication buttons
                OpRW_Buttons(false);
            }
        }

        private void bWrite_Click(object sender, EventArgs e)
        {
            int address = (int)nRegAddressWrite.Value;

            
            WriteReg(address, GetWriteReg(address));
        }

        private UInt16 GetWriteReg00 ()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckSsrArdu.Checked,      // Bit 0
                ckSsrDfc.Checked,       // Bit 1
                ckSsrHeu.Checked,       // Bit 2
                ckSsrC_Esm.Checked,     // Bit 3
                ckSsrAfe.Checked,       // Bit 4
                ckSsrEsrA.Checked,      // Bit 5
                ckSsrEsrB.Checked,      // Bit 6
                ckSsrService.Checked,   // Bit 7
                ckSsrUps2Occ.Checked,   // Bit 8
                ckSsrOcc1.Checked,      // Bit 9
                ckSsrOcc2.Checked,      // Bit 10
                ckSsrOcc3.Checked,      // Bit 11
                ckSsrHfMon.Checked,     // Bit 12
                ckSsrLampTest.Checked,  // Bit 13
                false,                  // Bit 14
                false                   // Bit 15
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[2];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)(array[0] | (array[1] << 8));
            // The copied ushort array now holds the UInt16 value
            return result ;
        }

        private UInt16 GetWriteReg80()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckReg80p0.Checked,          // Bit 0
                ckReg80p1.Checked,          // Bit 1
                ckReg80p2.Checked,          // Bit 2
                ckReg80p3.Checked,          // Bit 3
                ckReg80p4.Checked,          // Bit 4
                ckReg80p5.Checked,          // Bit 5
                ckReg80p6.Checked,          // Bit 6
                ckReg80p7.Checked,          // Bit 7
                ckReg80p8.Checked,          // Bit 8
                ckReg80p9.Checked,          // Bit 9
                false,                      // Bit 10
                false,                      // Bit 11
                false,                      // Bit 12
                false,                      // Bit 13
                false,                      // Bit 14
                false                       // Bit 15
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[2];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)(array[0] | (array[1] << 8));
            // The copied ushort array now holds the UInt16 value
            return result;
        }

        private UInt16 GetWriteReg31()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckReg31p0.Checked,          // Bit 0
                ckReg31p1.Checked,          // Bit 1
                false,                      // Bit 2
                false,                      // Bit 3
                false,                      // Bit 4
                false,                      // Bit 5
                false,                      // Bit 6
                false,                      // Bit 7
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[1];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)array[0];
            // The copied ushort array now holds the UInt16 value
            return result;
        }

        private UInt16 GetWriteReg33()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckReg33p0.Checked,          // Bit 0
                ckReg33p1.Checked,          // Bit 1
                false,                      // Bit 2
                false,                      // Bit 3
                false,                      // Bit 4
                false,                      // Bit 5
                false,                      // Bit 6
                false,                      // Bit 7
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[1];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)array[0];
            // The copied ushort array now holds the UInt16 value
            return result;
        }

        private UInt16 GetWriteReg9F()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckReg9Fp0.Checked,          // Bit 0
                ckReg9Fp1.Checked,          // Bit 1
                false,                      // Bit 2
                false,                      // Bit 3
                false,                      // Bit 4
                false,                      // Bit 5
                false,                      // Bit 6
                false,                      // Bit 7
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[1];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)array[0];
            // The copied ushort array now holds the UInt16 value
            return result;
        }

        private UInt16 GetWriteRegB1()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckSelAnP0.Checked,          // Bit 0
                ckSelAnP1.Checked,          // Bit 1
                ckSelAnP2.Checked,          // Bit 2
                ckSelProcP0.Checked,        // Bit 3
                ckSelProcP1.Checked,        // Bit 4
                ckSelProcP2.Checked,        // Bit 5
                ckSelProcP3.Checked,        // Bit 6
                ckSelProcF0.Checked,        // Bit 7
                ckSelProcF1.Checked,        // Bit 8
                false,                      // Bit 9
                false,                      // Bit 10
                false,                      // Bit 11
                false,                      // Bit 12
                false,                      // Bit 13
                false,                      // Bit 14
                false                       // Bit 15
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[2];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)(array[0] | (array[1] << 8));
            // The copied ushort array now holds the UInt16 value
            return result;
        }

        private UInt16 GetWriteRegB0()
        {
            BitArray bitArray = new BitArray(new bool[]
            {
                ckForceMux.Checked,         // Bit 0
                ckForceFan.Checked,         // Bit 1
                false,                      // Bit 2
                false,                      // Bit 3
                false,                      // Bit 4
                false,                      // Bit 5
                false,                      // Bit 6
                false,                      // Bit 7
                false,                      // Bit 8
                false,                      // Bit 9
                false,                      // Bit 10
                false,                      // Bit 11
                false,                      // Bit 12
                false,                      // Bit 13
                false,                      // Bit 14
                false                       // Bit 15
            });
            // Create an array of ushort to hold the copied data
            byte[] array = new byte[2];
            // Copy the BitArray to the ushort array
            bitArray.CopyTo(array, 0);
            UInt16 result = (ushort)(array[0] | (array[1] << 8));
            // The copied ushort array now holds the UInt16 value
            return result;
        }

            // acquire the value of register to be written from GUI
            int GetWriteReg(int address)
        {
            int val;
            switch (address)
            {
                case 0:
                    val = (int)GetWriteReg00(); //(int)nReg00.Value;
                    break;
                case 0x31:
                    val = (int)GetWriteReg31(); //(int)nReg00.Value;
                    break;
                case 0x33:
                    val = (int)GetWriteReg33(); //(int)nReg00.Value;
                    break;
                case 0x54:
                    val = (int)nReg54.Value;
                    break;
                case 0x55:
                    val = (int)nReg55.Value;
                    break;
                case 0x5A:
                    val = (int)nReg5A.Value;
                    break;
                case 0x5B:
                    val = (int)nReg5B.Value;
                    break;
                case 0x5C:
                    val = (int)nReg5C.Value;
                    break;
                case 0x61:
                    val = Convert.ToInt32(tReg61.Text, 16);
                    break;
                case 0x62:
                    val = Convert.ToInt32(tReg62.Text, 16);
                    break;
                case 0x63:
                    val = Convert.ToInt32(tReg63.Text, 16);
                    break;
                case 0x64:
                    val = Convert.ToInt32(tReg64.Text, 16);
                    break;
                case 0x65:
                    val = Convert.ToInt32(tReg65.Text, 16);
                    break;
                case 0x66:
                    val = Convert.ToInt32(tReg66.Text, 16);
                    break;
                case 0x67:
                    val = (int)nReg67.Value;
                    break;
                case 0x68:
                    val = (int)nReg68.Value;
                    break;
                case 0x6C:
                    val = (int)nReg6C.Value;
                    break;
                case 0x6D:
                    val = (int)nReg6D.Value;
                    break;
                case 0x6E:
                    val = (int)nReg6E.Value;
                    break;
                case 0x70:
                    val = (int)nReg70.Value;
                    break;
                case 0x71:
                    val = Convert.ToInt32(tReg71.Text, 16);
                    break;
                case 0x72:
                    val = Convert.ToInt32(tReg72.Text, 16);
                    break;
                case 0x73:
                    val = Convert.ToInt32(tReg73.Text, 16);
                    break;
                case 0x74:
                    val = Convert.ToInt32(tReg74.Text, 16);
                    break;
                case 0x75:
                    val = Convert.ToInt32(tReg75.Text, 16);
                    break;
                case 0x76:
                    val = Convert.ToInt32(tReg76.Text, 16);
                    break;
                case 0x77:
                    val = Convert.ToInt32(tReg77.Text, 16);
                    break;
                case 0x78:
                    val = (int)nReg78.Value;
                    break;
                case 0x79:
                    val = (int)nReg79.Value;
                    break;
                case 0x7A:
                    val = (int)nReg7A.Value;
                    break;
                case 0x7B:
                    val = (int)nReg7B.Value;
                    break;
                case 0x7F:
                    val = (int)nReg7F.Value;
                    break;
                case 0x80:
                    val = (int)GetWriteReg80(); //(int)nReg80.Value;
                    break;
                case 0x9F:
                    val = (int)GetWriteReg9F(); //(int)nReg00.Value;
                    break;
                case 0xA3:
                    val = (int)nRegA3.Value;
                    break;
                case 0xA4:
                    val = (int)nRegA4.Value;
                    break;
                case 0xB0:
                    val = (int)GetWriteRegB0(); //(int)nReg60.Value;
                    break;
                case 0xB1:
                    val = (int)GetWriteRegB1(); //(int)nReg68.Value;
                    break;
                case 0xB9:
                    val = (int)nRegB9.Value;
                    break;
                case 0xBA:
                    val = (int)nRegBA.Value;
                    break;
                default:
                    val = 0;
                    break;
            }
            return val;
        }
        private void textBoxSetAddress_TextChanged(object sender, EventArgs e)
        {
            try
            {
                UInt32 val = NetToNum(textBoxSetAddress.Text);
                tReg61.Text = $"{(val >> 16):X4}";
                tReg62.Text = $"{(val & 0xFFFF):X4}";
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void textBoxSetSubaddress_TextChanged(object sender, EventArgs e)
        {
            try
            {
                UInt32 val = NetToNum(textBoxSetSubaddress.Text);
                tReg63.Text = $"{(val >> 16):X4}";
                tReg64.Text = $"{(val & 0xFFFF):X4}";
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void textBoxSetGateway_TextChanged(object sender, EventArgs e)
        {
            try
            {
                UInt32 val = NetToNum(textBoxSetGateway.Text);
                tReg65.Text = $"{(val >> 16):X4}";
                tReg66.Text = $"{(val & 0xFFFF):X4}";
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private UInt32 NetToNum(string net)
        {
            try
            {
                string ipAddressString = net;

                // Parse the IP address
                IPAddress ipAddress = IPAddress.Parse(ipAddressString);

                // Get the bytes of the IP address
                byte[] ipBytes = ipAddress.GetAddressBytes();

                // Reverse the byte order if necessary (little-endian to big-endian)
                Array.Reverse(ipBytes);

                // Convert the byte array to a 32-bit unsigned integer
                UInt32 ipAddressAsNumber = BitConverter.ToUInt32(ipBytes, 0);

                // Convert the 32-bit unsigned integer to hex string with "0x" prefix and leading zeroes
                //string hexAddress = $"0x{ipAddressAsNumber:X8}";

                return ipAddressAsNumber;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void bWriteMul_Click(object sender, EventArgs e)
        {
            int address = (int)nRegAddressWriteMul.Value;
            int[] data = new int[(int)nRegNumWriteMul.Value];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = GetWriteReg(address + i);
            }
            WriteMulRegs(address, data);
        }

        private void bRead9408Y_Click(object sender, EventArgs e)
        {
            ReadRegs(0x4, 44);
            UpdateGui();
        }

        private void bRead9800Y_Click(object sender, EventArgs e)
        {
            ReadRegs(0x84, 0xA6 - 0x84 + 1);
            UpdateGui();
        }

        private void bReadAux_Click(object sender, EventArgs e)
        {
            ReadRegs(0x60, 0x6D - 0x60 + 1);
            UpdateGui();
        }

        private void bWrite9408Y_Click(object sender, EventArgs e)
        {
            //int address = 0x0;
            //int[] data = new int[3];
            //for (int i = 0; i < data.Length; i++)
            //{
            //    data[i] = GetWriteReg(address + i);
            //}
            //WriteMulRegs(address, data);
        }

        private void bWrite9800Y_Click(object sender, EventArgs e)
        {
            WriteReg(0x9F, GetWriteReg(0x9F));
        }

        private void bWriteTemEn_Click(object sender, EventArgs e)
        {
            WriteReg(0xA3, GetWriteReg(0xA3));
            WriteReg(0xA4, GetWriteReg(0xA4));
        }

        private void bSetAddress_Click(object sender, EventArgs e)
        {
            int address = 0x61;
            int[] data = new int[7];

            // manually invoke the text change events to update the Registers.
            textBoxSetAddress_TextChanged(textBoxSetAddress, EventArgs.Empty);
            textBoxSetSubaddress_TextChanged(textBoxSetSubaddress, EventArgs.Empty);
            textBoxSetGateway_TextChanged(textBoxSetGateway, EventArgs.Empty);

            // set register(in GUI) to save IP to FLASH
            nReg67.Value = 1;
            
            // fetch GUI register values 
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = GetWriteReg(address + i);
            }
            WriteMulRegs(address, data);
        }

        private async void bResetCpu_Click(object sender, EventArgs e)
        {
            WriteReg(0x68, 1);
            await Task.Delay(1000);
            bConnect_Click(null, EventArgs.Empty);
        }

        private void bWrite9408Discrete_Click(object sender, EventArgs e)
        {
            int address = 0x0;
            int[] data = new int[1];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = GetWriteReg(address + i);
            }
            WriteMulRegs(address, data);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //timer1.Interval = (int)numUpdateCycle.Value;
            //UpdateGui();
            
        }

        private void Reg00ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val& 0xFFFF) }; 
            BitArray bitArray = new BitArray(val16);
            rSsrArdu.Checked        = bitArray[0];
            rSsrDfc.Checked         = bitArray[1];
            rSsrHeu.Checked         = bitArray[2];
            rSsrC_Esm.Checked       = bitArray[3];
            rSsrAfe.Checked         = bitArray[4];
            rSsrEsrA.Checked        = bitArray[5];
            rSsrEsrB.Checked        = bitArray[6];
            rSsrService.Checked     = bitArray[7];
            rSsrUps2Occ.Checked     = bitArray[8];
            rSsrOcc1.Checked        = bitArray[9];
            rSsrOcc2.Checked        = bitArray[10];
            rSsrOcc3.Checked        = bitArray[11];
            rSsrHfMon.Checked       = bitArray[12];
            rSsrLampTest.Checked    = bitArray[13];
        }

        private void Reg01ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rSwArdu.Checked         = bitArray[0];
            rSwDfc.Checked          = bitArray[1];
            rSwHeu.Checked          = bitArray[2];
            rSwC_Esm.Checked        = bitArray[3];
            rSwAfe.Checked          = bitArray[4];
            rSwEsrA.Checked         = bitArray[5];
            rSwEsrB.Checked         = bitArray[6];
            rSwService.Checked      = bitArray[7];
            rSwUps2Occ.Checked      = bitArray[8];
            rSwOcc1.Checked         = bitArray[9];
            rSwOcc2.Checked         = bitArray[10];
            rSwOcc3.Checked         = bitArray[11];
            rSwHfMon.Checked        = bitArray[12];
            rSwAbjb.Checked         = bitArray[13];
            rSwGpu.Checked          = bitArray[14];
            rSwLampTest.Checked     = bitArray[15];
        }

        private void Reg02ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rLedFail.Checked        = bitArray[0];
            rLedOk.Checked          = bitArray[1];
            rLedHvpFail.Checked     = bitArray[2];
            rLedSystemOk.Checked    = bitArray[3];
            rLedPsCbFail.Checked    = bitArray[4];
            rLedSnsStackTemp.Checked = bitArray[5];
            rLedFanInd.Checked      = bitArray[6];
            rLedHeuStatus.Checked   = bitArray[7];
            rLedEsrB.Checked        = bitArray[8];
            rLedHfMon.Checked       = bitArray[9];
            rLedEsrA.Checked        = bitArray[10];
            rLedAbjb.Checked        = bitArray[11];
            rLedSpare.Checked       = bitArray[12];
            rLedMainOnOff.Checked   = bitArray[13];
        }

        private void bRead9408Discrete_Click(object sender, EventArgs e)
        {
            ReadRegs(0x0, 0x4);
            UpdateGui();
        }

        private void Reg03ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rCbMainOnOff.Checked = bitArray[0];
            rCb3pPlatform.Checked = bitArray[1];
            rCb1pPlatform.Checked = bitArray[2];
            rCbUps1.Checked = bitArray[3];
            rCbUps2.Checked = bitArray[4];
            rCbUps3.Checked = bitArray[5];
            rCbUps4.Checked = bitArray[6];
        }

        private void bReadMaint_Click(object sender, EventArgs e)
        {
            ReadRegs(0xB0, 0xBC - 0xB0 + 1);
            UpdateGui();
        }

        private void bWriteMaint_Click(object sender, EventArgs e)
        {
            int[] data = new int[2];
            
            data[0] = GetWriteReg(0xB0);
            data[1] = GetWriteReg(0xB1);
            WriteMulRegs(0xB0, data);

            data[0] = GetWriteReg(0xB9);
            data[1] = GetWriteReg(0xBA);
            WriteMulRegs(0xB9, data);

        }

        private void bRead9408Y_2_Click(object sender, EventArgs e)
        {
            ReadRegs(0x30, 0x52 - 0x30 +1);
            UpdateGui();
        }

        private void bWrite9408Y_2_Click(object sender, EventArgs e)
        {
            int[] data = new int[3]; 
            data[0] = GetWriteReg(0x31);
            data[1] = 0;
            data[2] = GetWriteReg(0x33);
            WriteMulRegs(0x31, data);
        }

        private void radioButton58_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void RegB0ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rForceMux.Checked = bitArray[0];
            rForceFan.Checked = bitArray[1];
        }
        private void RegB1ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rSelAnP0.Checked = bitArray[0];
            rSelAnP1.Checked = bitArray[1];
            rSelAnP2.Checked = bitArray[2];
            rSelProcP0.Checked = bitArray[3];
            rSelProcP1.Checked = bitArray[4];
            rSelProcP2.Checked = bitArray[5];
            rSelProcP3.Checked = bitArray[6];
            rSelProcF0.Checked = bitArray[7];
            rSelProcF1.Checked = bitArray[8];
        }

        private void Reg31ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            ckReg31p0.Checked = bitArray[0];
            ckReg31p1.Checked = bitArray[1];
        }

        private void Reg33ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            ckReg33p0.Checked = bitArray[0];
            ckReg33p1.Checked = bitArray[1];
        }

        private void Reg53ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg53p0.Checked = bitArray[0];
            rReg53p1.Checked = bitArray[1];
            rReg53p2.Checked = bitArray[2];
            rReg53p3.Checked = bitArray[3];
            rReg53p4.Checked = bitArray[4];
            rReg53p5.Checked = bitArray[5];
            rReg53p6.Checked = bitArray[6];
            rReg53p7.Checked = bitArray[7];
            rReg53p8.Checked = bitArray[8];
            rReg53p9.Checked = bitArray[9];
            rReg53p10.Checked = bitArray[10];
            rReg53p11.Checked = bitArray[11];
            rReg53p12.Checked = bitArray[12];
            rReg53p13.Checked = bitArray[13];
            rReg53p14.Checked = bitArray[14];
        }

        private void Reg56ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg56p0.Checked = bitArray[0];
            rReg56p1.Checked = bitArray[1];
            rReg56p2.Checked = bitArray[2];
            rReg56p3.Checked = bitArray[3];
            rReg56p4.Checked = bitArray[4];
            rReg56p5.Checked = bitArray[5];
            rReg56p6.Checked = bitArray[6];
            rReg56p7.Checked = bitArray[7];
            rReg56p8.Checked = bitArray[8];
            rReg56p9.Checked = bitArray[9];
            rReg56p10.Checked = bitArray[10];
            rReg56p11.Checked = bitArray[11];
            rReg56p12.Checked = bitArray[12];
            rReg56p13.Checked = bitArray[13];
            rReg56p14.Checked = bitArray[14];
            rReg56p15.Checked = bitArray[15];
        }

        private void Reg57ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg57p0.Checked = bitArray[0];
            rReg57p1.Checked = bitArray[1];
            rReg57p2.Checked = bitArray[2];
            rReg57p3.Checked = bitArray[3];
            rReg57p4.Checked = bitArray[4];
            rReg57p5.Checked = bitArray[5];
            rReg57p6.Checked = bitArray[6];
        }

        private void Reg58ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg58p0.Checked = bitArray[0];
            rReg58p1.Checked = bitArray[1];
            rReg58p2.Checked = bitArray[2];
            rReg58p3.Checked = bitArray[3];
            rReg58p4.Checked = bitArray[4];
            rReg58p5.Checked = bitArray[5];
        }

        private void bRead9408Y_3_Click(object sender, EventArgs e)
        {
            ReadRegs(0x53, 0x5C - 0x53 + 1);
            UpdateGui();
        }

        private void bWrite9408Y_3_Click(object sender, EventArgs e)
        {
            WriteReg(0x54, GetWriteReg(0x54));
            WriteReg(0x55, GetWriteReg(0x55));
            WriteReg(0x5C, GetWriteReg(0x5C));
        }

        private void bWriteTem_Click(object sender, EventArgs e)
        {
            WriteReg(0x5A, GetWriteReg(0x5A));
            WriteReg(0x5B, GetWriteReg(0x5B));
        }

        private void bRead9800Dis_Click(object sender, EventArgs e)
        {
            ReadRegs(0x80, 0x83 - 0x80 + 1);
            UpdateGui();
        }

        private void bWrite9800Dis_Click(object sender, EventArgs e)
        {
            int data = GetWriteReg(0x80);
            WriteReg(0x80, data);
        }

        private void Reg59ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg59p0.Checked = bitArray[0];
            rReg59p1.Checked = bitArray[1];
            rReg59p2.Checked = bitArray[2];
        }

        private void RegA5ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rRegA5p0.Checked = bitArray[0];
            rRegA5p1.Checked = bitArray[1];
            rRegA5p2.Checked = bitArray[2];
            rRegA5p3.Checked = bitArray[3];
        }
        private void RegA6ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rRegA6p0.Checked = bitArray[0];
            rRegA6p1.Checked = bitArray[1];
            rRegA6p2.Checked = bitArray[2];
        }

        private void Reg80ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg80p0.Checked = bitArray[0];
            rReg80p1.Checked = bitArray[1];
            rReg80p2.Checked = bitArray[2];
            rReg80p3.Checked = bitArray[3];
            rReg80p4.Checked = bitArray[4];
            rReg80p5.Checked = bitArray[5];
            rReg80p6.Checked = bitArray[6];
            rReg80p7.Checked = bitArray[7];
            rReg80p8.Checked = bitArray[8];
            rReg80p9.Checked = bitArray[9];
        }

        private void Reg81ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg81p0.Checked = bitArray[0];
            rReg81p1.Checked = bitArray[1];
            rReg81p2.Checked = bitArray[2];
            rReg81p3.Checked = bitArray[3];
            rReg81p4.Checked = bitArray[4];
            rReg81p5.Checked = bitArray[5];
            rReg81p6.Checked = bitArray[6];
            rReg81p7.Checked = bitArray[7];
            rReg81p8.Checked = bitArray[8];
            rReg81p9.Checked = bitArray[9];
        }

        private void Reg82ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg82p0.Checked = bitArray[0];
            rReg82p1.Checked = bitArray[1];
            rReg82p2.Checked = bitArray[2];
            rReg82p3.Checked = bitArray[3];
            rReg82p4.Checked = bitArray[4];
            rReg83p0.Checked = bitArray[5];
            rReg83p1.Checked = bitArray[6];
        }

        private void bReadAuxEeprom_Click(object sender, EventArgs e)
        {
            ReadRegs(0x70, 0x7F - 0x70 + 1);
            UpdateGui();
        }

        private void Reg83ToGui(int val)
        {
            // Initialize a BitArray using the ushort array
            int[] val16 = new int[1] { (val & 0xFFFF) };
            BitArray bitArray = new BitArray(val16);
            rReg83p0.Checked = bitArray[0];
            rReg83p1.Checked = bitArray[1];
        }

        private void bWriteAuxEeprom_Click(object sender, EventArgs e)
        {
            int address = 0x70;
            int[] data = new int[0x7F - 0x70 + 1];


            // read SN
            int sn = Convert.ToInt32(tSnEeprom.Text);
            Regs[0x71] = (UInt16)(sn & 0xFFFF);
            Regs[0x72] = (UInt16)(sn >> 16);
            tReg71.Text = $"{Regs[0x71]:X4}";
            tReg72.Text = $"{Regs[0x72]:X4}";
            // read PN
            byte[] bytePad = new byte[10];
            byte[] bytePn = Encoding.ASCII.GetBytes(tPnEeprom.Text);
            int len = (10 > bytePn.Length) ? bytePn.Length : 10;
            Array.Copy(bytePn, bytePad, len);

            for (int i=0; i< 5; i++)
            {
                
                UInt16 num = (UInt16)((bytePad[i * 2]) + (bytePad[i * 2 + 1] << 8));
                Regs[0x73 + i] = num;
            }
            tReg73.Text = $"{Regs[0x73]:X4}";
            tReg74.Text = $"{Regs[0x74]:X4}";
            tReg75.Text = $"{Regs[0x75]:X4}";
            tReg76.Text = $"{Regs[0x76]:X4}";
            tReg77.Text = $"{Regs[0x77]:X4}";

            // set register(in GUI) to save to EEPROM
            nReg7F.Value = 1;

            // fetch GUI register values 
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = GetWriteReg(address + i);
            }
            WriteMulRegs(address, data);
        }

        private void bWrite6C6D_Click(object sender, EventArgs e)
        {
            int address = 0x6C;
            int[] data = new int[3];
            // fetch GUI register values 
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = GetWriteReg(address + i);
            }
            WriteMulRegs(address, data);

        }

        private void ckReg9Fp0_CheckedChanged(object sender, EventArgs e)
        {
            if (ckReg9Fp0.Checked) ckReg9Fp1.Checked = false;
        }

        private void ckReg9Fp1_CheckedChanged(object sender, EventArgs e)
        {
            if (ckReg9Fp1.Checked) ckReg9Fp0.Checked = false;
        }

        private void ckRepeat_CheckedChanged(object sender, EventArgs e)
        {
            if (ckRepeat.Checked == true)
            {
                ckRepeat_En.Checked = false;
                bRead9408Discrete.Enabled = false;
                timer2.Enabled = true;
                timer2.Interval = (int)nRepeatDelay.Value;
            }
            else
            {
                bRead9408Discrete.Enabled = true;
                if (ckRepeat_En.Checked == false)
                    timer2.Enabled = false;
            }
        }

        private void ckRepeat_En_CheckedChanged(object sender, EventArgs e)
        {
            if (ckRepeat_En.Checked == true)
            {
                ckRepeat.Checked = false;
                bRead9800Dis.Enabled = false;
                timer2.Enabled = true;
                timer2.Interval = (int)nRepeatDelay.Value;
            }
            else
            {
                bRead9800Dis.Enabled = true;
                if (ckRepeat.Checked == false)
                    timer2.Enabled = false;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Interval = (int)nRepeatDelay.Value;
            if (ckRepeat.Checked)
                bRead9408Discrete_Click(null, null);
            else if (ckRepeat_En.Checked)
                bRead9800Dis_Click(null, null);

        }

        private string ConvertUInt16ArrayToString(UInt16[] array, int start, int len)
        {
            // Initialize a char array to store extracted characters.
            char[] chars = new char[len * 2];

            // Extract characters from each UInt16 element.
            for (int i = 0; i < len; i++)
            {
                // Extract high 8 bits as the first character.
                chars[i * 2] = (char)(array[start + i] & 0xFF);
                // Extract low 8 bits as the second character.
                chars[i * 2 + 1] = (char)(array[start + i] >> 8);
            }

            // Create a string from the char array.
            return new string(chars);
        }

        private void UpdateGui()
        {
            Reg00ToGui(Regs[0x0]);
            Reg01ToGui(Regs[0x1]);
            Reg02ToGui(Regs[0x2]);
            Reg03ToGui(Regs[0x3]);
            tReg04.Text = Regs[0x4].ToString();
            tReg05.Text = Regs[0x5].ToString();
            tReg06.Text = Regs[0x6].ToString();
            tReg07.Text = Regs[0x7].ToString();
            tReg08.Text = Regs[0x8].ToString();
            tReg09.Text = Regs[0x9].ToString();
            tReg0A.Text = Regs[0xA].ToString();
            tReg0B.Text = Regs[0xB].ToString();
            tReg0C.Text = Regs[0xC].ToString();
            tReg0D.Text = Regs[0xD].ToString();
            tReg0E.Text = Regs[0xE].ToString();
            tReg0F.Text = Regs[0xF].ToString();

            tReg10.Text = Regs[0x10].ToString();
            tReg11.Text = Regs[0x11].ToString();
            tReg12.Text = Regs[0x12].ToString();
            tReg13.Text = Regs[0x13].ToString();
            tReg14.Text = Regs[0x14].ToString();
            tReg15.Text = Regs[0x15].ToString();
            tReg16.Text = Regs[0x16].ToString();
            tReg17.Text = Regs[0x17].ToString();
            tReg18.Text = Regs[0x18].ToString();
            tReg19.Text = Regs[0x19].ToString();
            tReg1A.Text = Regs[0x1A].ToString();
            tReg1B.Text = Regs[0x1B].ToString();
            tReg1C.Text = Regs[0x1C].ToString();
            tReg1D.Text = Regs[0x1D].ToString();
            tReg1E.Text = Regs[0x1E].ToString();
            tReg1F.Text = Regs[0x1F].ToString();

            tReg20.Text = Regs[0x20].ToString();
            tReg21.Text = Regs[0x21].ToString();
            tReg22.Text = Regs[0x22].ToString();
            tReg23.Text = Regs[0x23].ToString();
            tReg24.Text = Regs[0x24].ToString();
            tReg25.Text = Regs[0x25].ToString();
            tReg26.Text = Regs[0x26].ToString();
            //tReg27.Text = Regs[0x27].ToString();
            //tReg28.Text = Regs[0x28].ToString();
            tReg29.Text = Regs[0x29].ToString();
            tReg2A.Text = Regs[0x2A].ToString();
            //tReg2B.Text = Regs[0x2B].ToString();
            //tReg2C.Text = Regs[0x2C].ToString();
            tReg2D.Text = Regs[0x2D].ToString();
            tReg2E.Text = Regs[0x2E].ToString();
            tReg2F.Text = Regs[0x2F].ToString();
            tReg30.Text = Regs[0x30].ToString();
            //tReg31.Text = Regs[0x31].ToString();
            Reg31ToGui(Regs[0x31]);
            tReg32.Text = Regs[0x32].ToString();
            //tReg33.Text = Regs[0x33].ToString();
            Reg33ToGui(Regs[0x33]);
            tReg34.Text = Regs[0x34].ToString();
            tReg35.Text = Regs[0x35].ToString();
            tReg36.Text = Regs[0x36].ToString();
            tReg37.Text = Regs[0x37].ToString();
            tReg38.Text = Regs[0x38].ToString();
            tReg39.Text = Regs[0x39].ToString();
            tReg3A.Text = Regs[0x3A].ToString();
            tReg3B.Text = Regs[0x3B].ToString();
            tReg3C.Text = Regs[0x3C].ToString();
            tReg3D.Text = Regs[0x3D].ToString();
            tReg3E.Text = Regs[0x3E].ToString();
            tReg3F.Text = Regs[0x3F].ToString();
            tReg40.Text = Regs[0x40].ToString();
            tReg41.Text = Regs[0x41].ToString();
            tReg42.Text = Regs[0x42].ToString();
            tReg43.Text = Regs[0x43].ToString();
            tReg44.Text = Regs[0x44].ToString();
            tReg45.Text = Regs[0x45].ToString();
            tReg46.Text = Regs[0x46].ToString();
            tReg47.Text = Regs[0x47].ToString();
            tReg48.Text = Regs[0x48].ToString();
            tReg49.Text = Regs[0x49].ToString();
            tReg4A.Text = Regs[0x4A].ToString();
            tReg4B.Text = Regs[0x4B].ToString();
            tReg4C.Text = Regs[0x4C].ToString();
            tReg4D.Text = Regs[0x4D].ToString();
            tReg4E.Text = Regs[0x4E].ToString();
            tReg4F.Text = Regs[0x4F].ToString();
            tReg50.Text = Regs[0x50].ToString();
            tReg51.Text = Regs[0x51].ToString();
            tReg52.Text = Regs[0x52].ToString();
            //tReg53.Text = Regs[0x43].ToString();
            Reg53ToGui(Regs[0x53]);
            tReg54.Text = Regs[0x54].ToString();
            nReg55.Value = Regs[0x55];
            //tReg56.Text = Regs[0x46].ToString();
            //tReg57.Text = Regs[0x47].ToString();
            //tReg58.Text = Regs[0x48].ToString();
            //tReg59.Text = Regs[0x49].ToString();
            Reg56ToGui(Regs[0x56]);
            Reg57ToGui(Regs[0x57]);
            Reg58ToGui(Regs[0x58]);
            Reg59ToGui(Regs[0x59]);
            tReg5A.Text = Regs[0x5A].ToString();
            tReg5B.Text = Regs[0x5B].ToString();
            tReg5C.Text = Regs[0x5D].ToString();

            tReg60.Text = Regs[0x60].ToString();
            tReg61.Text = $"{Regs[0x61]:X4}";
            tReg62.Text = $"{Regs[0x62]:X4}";
            tReg63.Text = $"{Regs[0x63]:X4}";
            tReg64.Text = $"{Regs[0x64]:X4}";
            tReg65.Text = $"{Regs[0x65]:X4}";
            tReg66.Text = $"{Regs[0x66]:X4}";
            nReg67.Value = Regs[0x67];
            nReg68.Value = Regs[0x68];
            tReg69.Text = Regs[0x69].ToString();
            tReg6A.Text = Regs[0x6A].ToString();
            tReg6B.Text = Regs[0x6B].ToString();
            nReg6C.Value = Regs[0x6C];
            nReg6D.Value = Regs[0x6D];
            tReg70.Text = Regs[0x70].ToString();
            tReg71.Text = $"{Regs[0x71]:X4}";
            tReg72.Text = $"{Regs[0x72]:X4}";
            tReg73.Text = $"{Regs[0x73]:X4}";
            tReg74.Text = $"{Regs[0x74]:X4}";
            tReg75.Text = $"{Regs[0x75]:X4}";
            tReg76.Text = $"{Regs[0x76]:X4}";
            tReg77.Text = $"{Regs[0x77]:X4}";
            tReg78.Text = Regs[0x78].ToString();
            tReg79.Text = Regs[0x79].ToString();
            tReg7A.Text = Regs[0x7A].ToString();
            tReg7B.Text = Regs[0x7B].ToString();
            nReg7F.Value = Regs[0x7F];

            tPsuConfEeprom.Text = (Regs[0x70] == 1) ? "9408Y" : (Regs[0x70] == 2) ? "9800Y" : "NA";
            tSnEeprom.Text = ((UInt32)(Regs[0x71] + (Regs[0x72] << 16))).ToString();
            tPnEeprom.Text = ConvertUInt16ArrayToString(Regs, 0x73, 5);
           

            Reg80ToGui(Regs[0x80]);
            Reg81ToGui(Regs[0x81]);
            Reg82ToGui(Regs[0x82]);
            Reg83ToGui(Regs[0x83]);
            tReg84.Text = Regs[0x84].ToString();
            tReg85.Text = Regs[0x85].ToString();
            tReg86.Text = Regs[0x86].ToString();
            tReg87.Text = Regs[0x87].ToString();
            tReg88.Text = Regs[0x88].ToString();
            tReg89.Text = Regs[0x89].ToString();
            tReg8A.Text = Regs[0x8A].ToString();
            tReg8B.Text = Regs[0x8B].ToString();
            tReg8C.Text = Regs[0x8C].ToString();
            tReg8D.Text = Regs[0x8D].ToString();
            tReg8E.Text = Regs[0x8E].ToString();
            tReg8F.Text = Regs[0x8F].ToString();
            tReg90.Text = Regs[0x90].ToString();
            tReg91.Text = Regs[0x91].ToString();
            tReg92.Text = Regs[0x92].ToString();
            tReg93.Text = Regs[0x93].ToString();
            tReg94.Text = Regs[0x94].ToString();
            tReg95.Text = Regs[0x95].ToString();
            tReg96.Text = Regs[0x96].ToString();
            tReg97.Text = Regs[0x97].ToString();
            tReg98.Text = Regs[0x98].ToString();
            tReg99.Text = Regs[0x99].ToString();
            tReg9A.Text = Regs[0x9A].ToString();
            tReg9B.Text = Regs[0x9B].ToString();
            tReg9C.Text = Regs[0x9C].ToString();
            tReg9D.Text = Regs[0x9D].ToString();
            tReg9E.Text = Regs[0x9E].ToString();
            tRegA0.Text = Regs[0xA0].ToString();
            tRegA1.Text = Regs[0xA1].ToString();
            tRegA2.Text = Regs[0xA2].ToString();
            tRegA3.Text = Regs[0xA3].ToString();
            tRegA4.Text = Regs[0xA4].ToString();
            RegA5ToGui(Regs[0xA5]);
            RegA6ToGui(Regs[0xA6]);


            //nReg5F.Value = Regs[0x60];
            RegB0ToGui(Regs[0xB0]);
            RegB1ToGui(Regs[0xB1]);
            tRegB2.Text = Regs[0xB2].ToString();
            tRegB3.Text = Regs[0xB3].ToString();
            tRegB4.Text = Regs[0xB4].ToString();
            tRegB5.Text = Regs[0xB5].ToString();
            tRegB6.Text = Regs[0xB6].ToString();
            tRegB7.Text = Regs[0xB7].ToString();
            tRegB8.Text = Regs[0xB8].ToString();
            tRegB9.Text = Regs[0xB9].ToString();
            tRegBA.Text = Regs[0xBA].ToString();
            tRegBB.Text = Regs[0xBB].ToString();
            tRegBC.Text = Regs[0xBC].ToString();

            tReceiveCount.Text = receiveCount.ToString();
            tSendCount.Text = sendCount.ToString();

        }
        


    }
}
