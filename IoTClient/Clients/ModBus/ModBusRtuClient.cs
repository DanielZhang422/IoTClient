﻿using IoTClient.Common.Helpers;
using IoTClient.Enums;
using IoTClient.Models;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace IoTClient.Clients.ModBus
{
    /// <summary>
    /// ModBusRtu协议客户端
    /// </summary>
    public class ModBusRtuClient : SerialPortBase, IModBusClient
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="portName">COM端口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">奇偶校验</param>
        public ModBusRtuClient(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
        {
            if (serialPort == null) serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;
            serialPort.Encoding = Encoding.ASCII;
            serialPort.Parity = parity;
#if !DEBUG
            serialPort.ReadTimeout = 1000;//1秒
#endif

        }

        #region 发送报文，并获取响应报文
        /// <summary>
        /// 发送报文，并获取响应报文
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public byte[] SendPackage(byte[] command)
        {
            //发送命令
            serialPort.Write(command, 0, command.Length);
            //获取响应报文
            return SerialPortRead(serialPort);
        }
        #endregion

        #region  Read 读取
        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="readLength">读取长度</param>
        /// <returns></returns>
        public Result<byte[]> Read(string address, byte stationNumber = 1, byte functionCode = 3, ushort readLength = 1)
        {
            if (isAutoOpen) Connect();

            var result = new Result<byte[]>();
            try
            {
                //获取命令（组装报文）
                byte[] command = GetReadCommand(address, stationNumber, functionCode, readLength);
                var commandCRC16 = CRC16.GetCRC16(command);
                result.Requst = string.Join(" ", commandCRC16.Select(t => t.ToString("X2")));

                //发送命令并获取响应报文
                var responsePackage = SendPackage(commandCRC16);
                if (!responsePackage.Any())
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果为空";
                    return result;
                }
                else if (!CRC16.CheckCRC16(responsePackage))
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果CRC16验证失败";
                    //return result;
                }

                byte[] resultData = new byte[responsePackage.Length - 2 - 3];
                Array.Copy(responsePackage, 3, resultData, 0, resultData.Length);
                result.Response = string.Join(" ", responsePackage.Select(t => t.ToString("X2")));
                //4 获取响应报文数据（字节数组形式）                
                result.Value = resultData.Reverse().ToArray();
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.ErrList.Add(ex.Message);
            }
            finally
            {
                if (isAutoOpen) Dispose();
            }
            return result;
        }

        /// <summary>
        /// 读取Int16
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<short> ReadInt16(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode);
            var result = new Result<short>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToInt16(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取UInt16
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<ushort> ReadUInt16(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode);
            var result = new Result<ushort>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToUInt16(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取Int32
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<int> ReadInt32(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode, readLength: 2);
            var result = new Result<int>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToInt32(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取UInt32
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<uint> ReadUInt32(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode, readLength: 2);
            var result = new Result<uint>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToUInt32(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取Int64
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<long> ReadInt64(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode, readLength: 4);
            var result = new Result<long>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToInt64(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取UInt64
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<ulong> ReadUInt64(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode, readLength: 4);
            var result = new Result<ulong>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToUInt64(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取Float
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<float> ReadFloat(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode, readLength: 2);
            var result = new Result<float>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToSingle(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取Double
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<double> ReadDouble(string address, byte stationNumber = 1, byte functionCode = 3)
        {
            var readResut = Read(address, stationNumber, functionCode, readLength: 4);
            var result = new Result<double>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToDouble(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取线圈
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public Result<bool> ReadCoil(string address, byte stationNumber = 1, byte functionCode = 1)
        {
            var readResut = Read(address, stationNumber, functionCode);
            var result = new Result<bool>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToBoolean(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 读取离散
        /// </summary>
        /// <param name="address"></param>
        /// <param name="stationNumber"></param>
        /// <param name="functionCode"></param>
        /// <returns></returns>
        public Result<bool> ReadDiscrete(string address, byte stationNumber = 1, byte functionCode = 2)
        {
            var readResut = Read(address, stationNumber, functionCode);
            var result = new Result<bool>()
            {
                IsSucceed = readResut.IsSucceed,
                Err = readResut.Err,
                ErrList = readResut.ErrList,
                Requst = readResut.Requst,
                Response = readResut.Response,
            };
            if (result.IsSucceed)
                result.Value = BitConverter.ToBoolean(readResut.Value, 0);
            return result;
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<short> ReadInt16(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = addressInt - beginAddressInt;
                var byteArry = values.Skip(i * 2).Take(2).Reverse().ToArray();
                return new Result<short>
                {
                    Value = BitConverter.ToInt16(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<short>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<ushort> ReadUInt16(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = addressInt - beginAddressInt;
                var byteArry = values.Skip(i * 2).Take(2).Reverse().ToArray();
                return new Result<ushort>
                {
                    Value = BitConverter.ToUInt16(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<ushort>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<int> ReadInt32(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = (addressInt - beginAddressInt) / 2;
                var byteArry = values.Skip(i * 2 * 2).Take(2 * 2).Reverse().ToArray();
                return new Result<int>
                {
                    Value = BitConverter.ToInt32(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<int>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<uint> ReadUInt32(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = (addressInt - beginAddressInt) / 2;
                var byteArry = values.Skip(i * 2 * 2).Take(2 * 2).Reverse().ToArray();
                return new Result<uint>
                {
                    Value = BitConverter.ToUInt32(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<uint>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<long> ReadInt64(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = (addressInt - beginAddressInt) / 4;
                var byteArry = values.Skip(i * 2 * 4).Take(2 * 4).Reverse().ToArray();
                return new Result<long>
                {
                    Value = BitConverter.ToInt64(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<long>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<ulong> ReadUInt64(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = (addressInt - beginAddressInt) / 4;
                var byteArry = values.Skip(i * 2 * 4).Take(2 * 4).Reverse().ToArray();
                return new Result<ulong>
                {
                    Value = BitConverter.ToUInt64(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<ulong>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<float> ReadFloat(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = (addressInt - beginAddressInt) / 2;
                var byteArry = values.Skip(i * 2 * 2).Take(2 * 2).Reverse().ToArray();
                return new Result<float>
                {
                    Value = BitConverter.ToSingle(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<float>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<double> ReadDouble(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = (addressInt - beginAddressInt) / 4;
                var byteArry = values.Skip(i * 2 * 4).Take(2 * 4).Reverse().ToArray();
                return new Result<double>
                {
                    Value = BitConverter.ToDouble(byteArry, 0)
                };
            }
            catch (Exception ex)
            {
                return new Result<double>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<bool> ReadCoil(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = addressInt - beginAddressInt;
                var index = (i + 1) % 8 == 0 ? (i + 1) / 8 : (i + 1) / 8 + 1;
                var binaryArray = Convert.ToInt32(values[index - 1]).IntToBinaryArray().ToArray().Reverse().ToArray();
                var isBit = false;
                if ((index - 1) * 8 + binaryArray.Length > i)
                    isBit = binaryArray[i - (index - 1) * 8].ToString() == 1.ToString();
                return new Result<bool>()
                {
                    Value = isBit
                };
            }
            catch (Exception ex)
            {
                return new Result<bool>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 从批量读取的数据字节提取对应的地址数据
        /// </summary>
        /// <param name="beginAddress">批量读取的起始地址</param>
        /// <param name="address">读取地址</param>
        /// <param name="values">批量读取的值</param>
        /// <returns></returns>
        public Result<bool> ReadDiscrete(string beginAddress, string address, byte[] values)
        {
            if (!int.TryParse(address?.Trim(), out int addressInt) || !int.TryParse(beginAddress?.Trim(), out int beginAddressInt))
                throw new Exception($"只能是数字，参数address：{address}  beginAddress：{beginAddress}");
            try
            {
                var i = addressInt - beginAddressInt;
                var index = (i + 1) % 8 == 0 ? (i + 1) / 8 : (i + 1) / 8 + 1;
                var binaryArray = Convert.ToInt32(values[index - 1]).IntToBinaryArray().ToArray().Reverse().ToArray();
                var isBit = false;
                if ((index - 1) * 8 + binaryArray.Length > i)
                    isBit = binaryArray[i - (index - 1) * 8].ToString() == 1.ToString();
                return new Result<bool>()
                {
                    Value = isBit
                };
            }
            catch (Exception ex)
            {
                return new Result<bool>
                {
                    IsSucceed = false,
                    Err = ex.Message
                };
            }
        }

        /// <summary>
        /// 分批读取，默认按19个地址打包读取
        /// </summary>
        /// <param name="addresses">地址集合</param>
        /// <param name="batchNumber">批量读取数量</param>
        /// <returns></returns>
        public Result<Dictionary<string, object>> BatchRead(Dictionary<string, DataTypeEnum> addresses, int batchNumber = 19)
        {
            var result = new Result<Dictionary<string, object>>();
            result.Value = new Dictionary<string, object>();

            var batchCount = Math.Ceiling((float)addresses.Count / batchNumber);
            for (int i = 0; i < batchCount; i++)
            {
                var tempAddresses = addresses.Skip(i * batchNumber).Take(batchNumber).ToDictionary(t => t.Key, t => t.Value);
                var tempResult = BatchRead(tempAddresses);
                if (tempResult.IsSucceed)
                {
                    foreach (var item in tempResult.Value)
                    {
                        result.Value.Add(item.Key, item.Value);
                    }
                }
                else
                {
                    result.IsSucceed = false;
                    result.Err = tempResult.Err;
                }
            }
            return result.EndTime();
        }

        /// <summary>
        /// 最多只能批量读取19个数据？        
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns></returns>
        private Result<Dictionary<string, object>> BatchRead(Dictionary<string, DataTypeEnum> addresses)
        {
            var result = new Result<Dictionary<string, object>>();
            result.Value = new Dictionary<string, object>();
            try
            {
                foreach (var item in addresses)
                {
                    var richText = item.Key.Split('_');
                    if (richText.Length < 2)
                        continue; //必须传入站号
                    var stationNumber = byte.Parse(richText[0]);
                    var addresse = richText[1];
                    var functionCode = richText.Length >= 3 ? byte.Parse(richText[2]) : (byte)3;
                    object value;
                    switch (item.Value)
                    {
                        case DataTypeEnum.Bool:
                            value = ReadDiscrete(addresse, stationNumber, functionCode).Value;
                            break;
                        //case DataTypeEnum.Byte:
                        //    value = readResut[0];
                        //    break;
                        case DataTypeEnum.Int16:
                            value = ReadInt16(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.UInt16:
                            value = ReadUInt16(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.Int32:
                            value = ReadInt32(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.UInt32:
                            value = ReadUInt32(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.Int64:
                            value = ReadInt64(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.UInt64:
                            value = ReadUInt64(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.Float:
                            value = ReadFloat(addresse, stationNumber, functionCode).Value;
                            break;
                        case DataTypeEnum.Double:
                            value = ReadDouble(addresse, stationNumber, functionCode).Value;
                            break;
                        default:
                            throw new Exception($"未定义数据类型：{item.Value}");
                    }
                    result.Value.Add(item.Key, value);
                }
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.Exception = ex;
                result.ErrList.Add(ex.Message);
            }
            return result.EndTime();
        }
        #endregion

        #region Write 写入
        /// <summary>
        /// 线圈写入
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <param name="stationNumber"></param>
        /// <param name="functionCode"></param>
        public Result Write(string address, bool value, byte stationNumber = 1, byte functionCode = 5)
        {
            if (isAutoOpen) Connect();
            var result = new Result();
            try
            {
                var command = GetWriteCoilCommand(address, value, stationNumber, functionCode);
                var commandCRC16 = CRC16.GetCRC16(command);
                result.Requst = string.Join(" ", commandCRC16.Select(t => t.ToString("X2")));
                //发送命令并获取响应报文
                var responsePackage = SendPackage(commandCRC16);
                if (!responsePackage.Any())
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果为空";
                    return result;
                }
                else if (!CRC16.CheckCRC16(responsePackage))
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果CRC16验证失败";
                    //return result;
                }
                byte[] resultBuffer = new byte[responsePackage.Length - 2];
                Buffer.BlockCopy(responsePackage, 0, resultBuffer, 0, resultBuffer.Length);
                result.Response = string.Join(" ", responsePackage.Select(t => t.ToString("X2")));
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.ErrList.Add(ex.Message);
            }
            finally
            {
                if (isAutoOpen) Dispose();
            }
            return result;
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address"></param>
        /// <param name="values"></param>
        /// <param name="stationNumber"></param>
        /// <param name="functionCode"></param>
        /// <returns></returns>
        public Result Write(string address, byte[] values, byte stationNumber = 1, byte functionCode = 16)
        {
            if (isAutoOpen) Connect();

            var result = new Result();
            try
            {
                var command = GetWriteCommand(address, values, stationNumber, functionCode);

                var commandCRC16 = CRC16.GetCRC16(command);
                result.Requst = string.Join(" ", commandCRC16.Select(t => t.ToString("X2")));
                var responsePackage = SendPackage(commandCRC16);
                if (!responsePackage.Any())
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果为空";
                    return result;
                }
                else if (!CRC16.CheckCRC16(responsePackage))
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果CRC16验证失败";
                    //return result;
                }
                byte[] resultBuffer = new byte[responsePackage.Length - 2];
                Array.Copy(responsePackage, 0, resultBuffer, 0, resultBuffer.Length);
                result.Response = string.Join(" ", responsePackage.Select(t => t.ToString("X2")));
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.ErrList.Add(ex.Message);
            }
            finally
            {
                if (isAutoOpen) Dispose();
            }
            return result;
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, short value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, ushort value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, int value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, uint value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, long value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, ulong value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, float value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        public Result Write(string address, double value, byte stationNumber = 1, byte functionCode = 16)
        {
            var values = BitConverter.GetBytes(value).Reverse().ToArray();
            return Write(address, values, stationNumber, functionCode);
        }
        #endregion

        #region 获取命令

        /// <summary>
        /// 获取读取命令
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="length">读取长度</param>
        /// <returns></returns>
        public byte[] GetReadCommand(string address, byte stationNumber, byte functionCode, ushort length)
        {
            var readAddress = ushort.Parse(address?.Trim());
            byte[] buffer = new byte[6];
            buffer[0] = stationNumber;  //站号
            buffer[1] = functionCode;   //功能码
            buffer[2] = BitConverter.GetBytes(readAddress)[1];
            buffer[3] = BitConverter.GetBytes(readAddress)[0];//寄存器地址
            buffer[4] = BitConverter.GetBytes(length)[1];
            buffer[5] = BitConverter.GetBytes(length)[0];//表示request 寄存器的长度(寄存器个数)
            return buffer;
        }

        /// <summary>
        /// 获取写入命令
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="values"></param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public byte[] GetWriteCommand(string address, byte[] values, byte stationNumber, byte functionCode)
        {
            var writeAddress = ushort.Parse(address?.Trim());
            byte[] buffer = new byte[7 + values.Length];
            buffer[0] = stationNumber; //站号
            buffer[1] = functionCode;  //功能码
            buffer[2] = BitConverter.GetBytes(writeAddress)[1];
            buffer[3] = BitConverter.GetBytes(writeAddress)[0];//寄存器地址
            buffer[4] = (byte)(values.Length / 2 / 256);
            buffer[5] = (byte)(values.Length / 2 % 256);//写寄存器数量(除2是两个字节一个寄存器，寄存器16位。除以256是byte最大存储255。)              
            buffer[6] = (byte)(values.Length);          //写字节的个数
            values.CopyTo(buffer, 7);                   //把目标值附加到数组后面
            return buffer;
        }

        /// <summary>
        /// 获取线圈写入命令
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value"></param>
        /// <param name="stationNumber">站号</param>
        /// <param name="functionCode">功能码</param>
        /// <returns></returns>
        public byte[] GetWriteCoilCommand(string address, bool value, byte stationNumber, byte functionCode)
        {
            var writeAddress = ushort.Parse(address?.Trim());
            byte[] buffer = new byte[6];
            buffer[0] = stationNumber;//站号
            buffer[1] = functionCode; //功能码
            buffer[2] = BitConverter.GetBytes(writeAddress)[1];
            buffer[3] = BitConverter.GetBytes(writeAddress)[0];//寄存器地址
            buffer[4] = (byte)(value ? 0xFF : 0x00);     //此处只可以是FF表示闭合00表示断开，其他数值非法
            buffer[5] = 0x00;
            return buffer;
        }

        #endregion
    }
}
