﻿using System;
using System.Collections.Generic;
using LightJson;
using BGC.Mathematics;

namespace BGC.Audio.Audiometry
{
    public class ValidationResults
    {
        private int CURRENT_VERSION = 2;
        public int Version { get; }

        public DateTime ValidationDate { get; }

        public TransducerProfile TransducerProfile { get; }

        public ValidationFrequencyCollection Oscillator { get; }

        public ValidationFrequencyCollection PureTone { get; }
        public ValidationFrequencyCollection Narrowband { get; }
        public ValidationFrequencyCollection.ValidationLevelCollection Broadband { get; }

        public ValidationResults(TransducerProfile transducerProfile)
        {
            Version = CURRENT_VERSION;

            ValidationDate = DateTime.Now;

            TransducerProfile = transducerProfile;

            Oscillator = new ValidationFrequencyCollection();

            PureTone = new ValidationFrequencyCollection();
            Narrowband = new ValidationFrequencyCollection();
            Broadband = new ValidationFrequencyCollection.ValidationLevelCollection();
        }

        public ValidationResults(JsonObject data)
        {
            Version = data.ContainsKey("Version") ? data["Version"].AsInteger : 1;

            ValidationDate = data["ValidationDate"].AsDateTime.Value;

            TransducerProfile = new TransducerProfile(data["Transducer"]);

            if (Version > 1)
            {
                Oscillator = new ValidationFrequencyCollection(data["Oscillator"]);
            }

            PureTone = new ValidationFrequencyCollection(data["PureTone"]);
            Narrowband = new ValidationFrequencyCollection(data["Narrowband"]);
            Broadband = new ValidationFrequencyCollection.ValidationLevelCollection(data["Broadband"]);
        }

        public JsonObject Serialize() => new JsonObject()
        {
            ["Version"] = Version,

            ["ValidationDate"] = ValidationDate,

            ["Transducer"] = TransducerProfile.Serialize(),

            ["Oscillator"] = Oscillator.Serialize(),
            ["PureTone"] = PureTone.Serialize(),
            ["Narrowband"] = Narrowband.Serialize(),
            ["Broadband"] = Broadband.Serialize()
        };
    }

    public class ValidationFrequencyCollection
    {
        public List<FrequencyValidationPoint> Points { get; }

        public ValidationFrequencyCollection()
        {
            Points = new List<FrequencyValidationPoint>();
        }

        public ValidationFrequencyCollection(JsonArray data)
        {
            Points = new List<FrequencyValidationPoint>();

            foreach (JsonObject frequencyPoint in data)
            {
                Points.Add(new FrequencyValidationPoint(frequencyPoint));
            }
        }

        public void SetValidationPoint(
            double frequency,
            double levelHL,
            AudioChannel channel,
            double expectedRMS,
            double measuredLevelHL) =>
            GetLevelCollection(frequency)
            .SetValidationValue(levelHL, channel, expectedRMS, measuredLevelHL);

        private ValidationLevelCollection GetLevelCollection(double frequency)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                if (Points[i].Frequency == frequency)
                {
                    //Found target frequency
                    return Points[i].Levels;
                }

                if (Points[i].Frequency > frequency)
                {
                    //Passed target frequency - create new
                    Points.Insert(i, new FrequencyValidationPoint(frequency));
                    return Points[i].Levels;
                }
            }

            //Reached the end without finding it
            Points.Add(new FrequencyValidationPoint(frequency));
            return Points[Points.Count - 1].Levels;
        }

        public JsonArray Serialize()
        {
            JsonArray points = new JsonArray();
            foreach (FrequencyValidationPoint point in Points)
            {
                points.Add(point.Serialize());
            }

            return points;
        }

        public class FrequencyValidationPoint
        {
            public double Frequency { get; }
            public ValidationLevelCollection Levels { get; }

            public FrequencyValidationPoint(double frequency)
            {
                Frequency = frequency;
                Levels = new ValidationLevelCollection();
            }

            public FrequencyValidationPoint(JsonObject data)
            {
                Frequency = data["Frequency"];
                Levels = new ValidationLevelCollection(data["Levels"].AsJsonArray);
            }

            public JsonObject Serialize() => new JsonObject()
            {
                ["Frequency"] = Frequency,
                ["Levels"] = Levels.Serialize()
            };
        }


        public class ValidationLevelCollection
        {
            public List<ValidationPoint> Points { get; }

            public ValidationLevelCollection()
            {
                Points = new List<ValidationPoint>();
            }

            public ValidationLevelCollection(JsonArray data)
            {
                Points = new List<ValidationPoint>();

                foreach (JsonObject validationPoint in data)
                {
                    Points.Add(new ValidationPoint(validationPoint));
                }
            }

            public JsonArray Serialize()
            {
                JsonArray points = new JsonArray();
                foreach (ValidationPoint point in Points)
                {
                    points.Add(point.Serialize());
                }

                return points;
            }

            public void SetValidationValue(
                double levelHL,
                AudioChannel channel,
                double expectedRMS,
                double measuredLevelHL)
            {
                switch (channel)
                {
                    case AudioChannel.Left:
                        GetValidationPoint(levelHL).LeftExpectedRMS = expectedRMS;
                        GetValidationPoint(levelHL).LeftLevelHL = measuredLevelHL;
                        break;

                    case AudioChannel.Right:
                        GetValidationPoint(levelHL).RightExpectedRMS = expectedRMS;
                        GetValidationPoint(levelHL).RightLevelHL = measuredLevelHL;
                        break;

                    default:
                        throw new Exception($"Unexpected AudioChannel for Setting Validation {channel}");
                }

            }

            private ValidationPoint GetValidationPoint(double levelHL)
            {
                for (int i = 0; i < Points.Count; i++)
                {
                    if (Points[i].AttemptedLevelHL == levelHL)
                    {
                        //Found target level
                        return Points[i];
                    }

                    if (Points[i].AttemptedLevelHL > levelHL)
                    {
                        //Passed target level - create new
                        Points.Insert(i, new ValidationPoint(levelHL));
                        return Points[i];
                    }
                }

                //Reached the end without finding it
                Points.Add(new ValidationPoint(levelHL));
                return Points[Points.Count - 1];
            }

            public class ValidationPoint
            {
                public double AttemptedLevelHL { get; }

                public double LeftExpectedRMS { get; set; }
                public double RightExpectedRMS { get; set; }

                public double LeftLevelHL { get; set; }
                public double RightLevelHL { get; set; }

                public ValidationPoint(double levelHL)
                {
                    AttemptedLevelHL = levelHL;

                    LeftExpectedRMS = double.NaN;
                    RightExpectedRMS = double.NaN;

                    LeftLevelHL = double.NaN;
                    RightLevelHL = double.NaN;
                }

                public ValidationPoint(JsonObject data)
                {
                    AttemptedLevelHL = data["AttemptedLevelHL"];

                    LeftExpectedRMS = data.ContainsKey("LeftExpectedRMS") ? data["LeftExpectedRMS"].AsNumber : double.NaN;
                    RightExpectedRMS = data.ContainsKey("RightExpectedRMS") ? data["RightExpectedRMS"].AsNumber : double.NaN;

                    LeftLevelHL = data.ContainsKey("LeftLevelHL") ? data["LeftLevelHL"].AsNumber : double.NaN;
                    RightLevelHL = data.ContainsKey("RightLevelHL") ? data["RightLevelHL"].AsNumber : double.NaN;
                }

                public JsonObject Serialize()
                {
                    JsonObject data = new JsonObject()
                    {
                        ["AttemptedLevelHL"] = AttemptedLevelHL
                    };

                    if (!double.IsNaN(LeftExpectedRMS))
                    {
                        data.Add("LeftExpectedRMS", LeftExpectedRMS);
                    }

                    if (!double.IsNaN(RightExpectedRMS))
                    {
                        data.Add("RightExpectedRMS",RightExpectedRMS);
                    }

                    if (!double.IsNaN(LeftLevelHL))
                    {
                        data.Add("LeftLevelHL", LeftLevelHL);
                    }

                    if (!double.IsNaN(RightLevelHL))
                    {
                        data.Add("RightLevelHL", RightLevelHL);
                    }

                    return data;
                }
            }
        }
    }
}
