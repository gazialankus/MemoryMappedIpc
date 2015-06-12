using System;
using System.Collections.Generic;
using System.Text;

namespace MemoryMappedIpcServer
{
    internal class GyroCalibrator {
        //private List<byte> _activeCalibrationWids = new List<byte>(); 
        //private Dictionary<byte, List<short[]>> _gyroValues = new Dictionary<byte, List<short[]>>();
        private readonly Dictionary<byte, long[]> _gyroSums = new Dictionary<byte, long[]>();
        private readonly Dictionary<byte, int> _gyroCounts = new Dictionary<byte, int>();

        private readonly Dictionary<byte, short[]> _calibrationValues = new Dictionary<byte, short[]>();

        public bool IsCalibrationUnderwayFor(byte wid) {
            lock (_gyroSums) {
                return _gyroSums.ContainsKey(wid);
            }
        }

        public bool StartCalibrationDesired(byte wid) {
            lock (_gyroSums) {
                if (_gyroSums.ContainsKey(wid)) {
                    return false;
                } else {
                    _gyroSums[wid] = new long[3] { 0, 0, 0 };
                    _gyroCounts[wid] = 0;
                    return true;
                }
            }
        }

        public void EndCalibrationDesired(byte wid) {
            lock (_gyroSums) {
                int count = _gyroCounts[wid];
                if (count > 0) {
                    short x = (short)(_gyroSums[wid][0] / count);
                    short y = (short)(_gyroSums[wid][1] / count);
                    short z = (short)(_gyroSums[wid][2] / count);

                    lock (_calibrationValues) {
                        _calibrationValues[wid] = new short[] {x, y, z};
                    }
                }

                _gyroSums.Remove(wid);
                _gyroCounts.Remove(wid);
            }
        }

        public bool IsCalibrationValuesReadyFor(byte wid) {
            lock (_calibrationValues) {
                return _calibrationValues.ContainsKey(wid);
            }
        }

        public short[] ConsumeCalibrationValuesFor(byte wid) {
            lock (_calibrationValues) {
                short[] v = _calibrationValues[wid];
                _calibrationValues.Remove(wid);
                return v;
            }
        }

        public void RawGyroReceived(byte wmi, short xs, short ys, short zs) {
            // maybe it's not desired anymore. check it before you add.
            lock (_gyroSums) {
                if (_gyroSums.ContainsKey(wmi)) {
                    _gyroSums[wmi][0] += xs;
                    _gyroSums[wmi][1] += ys;
                    _gyroSums[wmi][2] += zs;
                    _gyroCounts[wmi] += 1;
                }
            }
        }
    }
}
