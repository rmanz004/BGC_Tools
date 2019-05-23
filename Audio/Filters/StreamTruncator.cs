﻿using System;
using System.Collections.Generic;
using UnityEngine;
using BGC.Mathematics;

namespace BGC.Audio.Filters
{
    /// <summary>
    /// Truncates underlying stream
    /// </summary>
    public class StreamTruncator : SimpleBGCFilter
    {
        public override int Channels => stream.Channels;

        public override int TotalSamples { get; }
        public override int ChannelSamples { get; }

        private readonly int sampleShift;

        private readonly bool recalculateRMS;

        public int Position { get; private set; }

        public StreamTruncator(
            IBGCStream stream,
            int totalChannelSamples = -1,
            int sampleShift = 0,
            bool recalculateRMS = false)
            : base(stream)
        {
            if (sampleShift > stream.ChannelSamples)
            {
                Debug.LogError("Requested a sampleOffset larger than clip length");
                sampleShift = 0;
            }

            this.sampleShift = sampleShift;

            if (totalChannelSamples != -1)
            {
                ChannelSamples = Math.Min(
                    totalChannelSamples,
                    stream.ChannelSamples - sampleShift);
                TotalSamples = Channels * ChannelSamples;
            }
            else
            {
                if (stream.ChannelSamples == int.MaxValue)
                {
                    ChannelSamples = int.MaxValue;
                    TotalSamples = int.MaxValue;
                }
                else
                {
                    ChannelSamples = stream.ChannelSamples - sampleShift;
                    TotalSamples = Channels * ChannelSamples;
                }
            }

            TotalSamples = Channels * ChannelSamples;

            this.recalculateRMS = recalculateRMS;

            Reset();
        }

        public StreamTruncator(
            IBGCStream stream,
            double totalDuration = double.NaN,
            int sampleShift = 0,
            bool recalculateRMS = false)
            : base(stream)
        {
            if (sampleShift > stream.ChannelSamples)
            {
                Debug.LogError("Requested a sampleOffset larger than clip length");
                sampleShift = 0;
            }

            this.sampleShift = sampleShift;

            if (!double.IsNaN(totalDuration))
            {
                ChannelSamples = Math.Min(
                    (int)Math.Round(totalDuration * SamplingRate),
                    stream.ChannelSamples - sampleShift);
                TotalSamples = Channels * ChannelSamples;
            }
            else
            {
                if (stream.ChannelSamples == int.MaxValue)
                {
                    ChannelSamples = int.MaxValue;
                    TotalSamples = int.MaxValue;
                }
                else
                {
                    ChannelSamples = stream.ChannelSamples - sampleShift;
                    TotalSamples = Channels * ChannelSamples;
                }
            }

            this.recalculateRMS = recalculateRMS;

            Reset();
        }

        public override void Reset()
        {
            Position = 0;
            stream.Reset();
            if (sampleShift > 0)
            {
                stream.Seek(sampleShift);
            }
        }

        public override void Seek(int position)
        {
            Position = GeneralMath.Clamp(position, 0, ChannelSamples);
            stream.Seek(Position + sampleShift);
        }

        public override int Read(float[] data, int offset, int count)
        {
            int remainingSamples = count;

            while (remainingSamples > 0 && Position < ChannelSamples)
            {
                int copyLength = Math.Min(Channels * (ChannelSamples - Position), remainingSamples);
                int readSamples = stream.Read(data, offset, copyLength);

                if (readSamples == 0)
                {
                    //No more samples
                    break;
                }

                remainingSamples -= readSamples;
                offset += readSamples;
                Position += readSamples / Channels;
            }

            return count - remainingSamples;
        }

        private IEnumerable<double> _channelRMS = null;
        public override IEnumerable<double> GetChannelRMS()
        {
            if (_channelRMS == null)
            {
                if (recalculateRMS)
                {
                    _channelRMS = this.CalculateRMS();
                }
                else
                {
                    _channelRMS = stream.GetChannelRMS();
                }
            }

            return _channelRMS;
        }
    }

}
