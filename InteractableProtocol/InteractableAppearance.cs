using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MongoDB.Bson.Serialization.Attributes;
using System.Threading;
using Lidgren.Network;

namespace InteractableProtocol
{
    [Serializable]
    public partial class InteractableAppearance : InteractableComponent, IEnumerable
    {
        [BsonElement("BodyParts")]
        private List<InteractableBodyPart> _bodyParts;
        private ReaderWriterLockSlim _rwLock;

        /// <summary>
        /// Returns the body part with type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public InteractableBodyPart this[BodyPart type]
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _bodyParts.First(bp => bp.Type == type);
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            set
            {
                _rwLock.EnterWriteLock();
                if (_bodyParts.Any(bp => bp.Type == type))
                    _bodyParts[_bodyParts.FindIndex(bp => bp.Type == type)] = value;
                else
                    _bodyParts.Add(value);

                _rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the body part at index
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public InteractableBodyPart this[Int32 index]
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _bodyParts[index];
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            set
            {
                _rwLock.EnterWriteLock();
                _bodyParts[index] = value;
                _rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the number of body parts
        /// </summary>
        public Int32 Count
        {
            get { return _bodyParts.Count; }
        }

        /// <summary>
        /// Map Dir coord
        /// </summary>
        [BsonRequired]
        public Byte MapDir
        {
            get;
            set;
        }

        /// <summary>
        /// Initial Animation Frame
        /// </summary>
        public Byte AnimationFrame
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [BsonRequired]
        public Byte Opacity
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Byte Hue
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public InteractableAppearance()
        {
            _rwLock = new ReaderWriterLockSlim();
            _bodyParts = new List<InteractableBodyPart>();
        }

        /// <summary>
        /// Generates body with one asset
        /// </summary>
        /// <param name="graphic"></param>
        /// <returns></returns>
        public static InteractableAppearance Generate(String graphic)
        {
            InteractableAppearance result = new InteractableAppearance();
            result.AddPart(InteractableBodyPart.Generate(BodyPart.Asset, graphic, 255, 0, 10));

            result.MapDir = 2;
            result.AnimationFrame = 0;
            result.Opacity = 255;
            result.Hue = 0;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static InteractableAppearance Generate(Interactable root, String graphic)
        {
            InteractableAppearance result = Generate(graphic);
            result.Root = root;
            return result;
        }

        /// <summary>
        /// Generates a body with skin, eyes, hair
        /// </summary>
        /// <param name="skintone"></param>
        /// <param name="eyeType"></param>
        /// <param name="eyeColor"></param>
        /// <param name="hairType"></param>
        /// <param name="hairColor"></param>
        /// <returns></returns>
        public static InteractableAppearance Generate(Byte skintone, Byte eyeType, Byte eyeColor, Byte hairType, Byte hairColor)
        {
            InteractableAppearance result = new InteractableAppearance();
            result.AddPart(InteractableBodyPart.GenerateSkin(skintone));
            result.AddPart(InteractableBodyPart.GenerateEyes(eyeType, eyeColor));
            result.AddPart(InteractableBodyPart.GenerateHair(hairType, hairColor));

            result.MapDir = 2;
            result.AnimationFrame = 0;
            result.Opacity = 255;
            result.Hue = 0;

            result.Sort();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static InteractableAppearance Generate(Interactable root, Byte skintone, Byte eyeType, Byte eyeColor, Byte hairType, Byte hairColor)
        {
            InteractableAppearance result = Generate(skintone, eyeType, eyeColor, hairType, hairColor);
            result.Root = root;
            return result;
        }

        /// <summary>
        /// Adds a part
        /// </summary>
        /// <param name="part"></param>
        public void AddPart(InteractableBodyPart part)
        {
            _rwLock.EnterWriteLock();
            _bodyParts.Add(part);
            _rwLock.ExitWriteLock();

            Sort();
        }

        /// <summary>
        /// Removes a part
        /// </summary>
        /// <param name="part"></param>
        public void RemovePart(InteractableBodyPart part)
        {
            _rwLock.EnterWriteLock();
            _bodyParts.Remove(part);
            _rwLock.ExitWriteLock();
        }

        /// <summary>
        /// Removes a part by type
        /// </summary>
        /// <param name="type"></param>
        public void RemovePart(BodyPart type)
        {
            _rwLock.EnterWriteLock();
            InteractableBodyPart pending = _bodyParts.First(bp => bp.Type == type);
            _bodyParts.Remove(pending);
            _rwLock.ExitWriteLock();
        }

        /// <summary>
        /// Sorts the body parts by priority
        /// </summary>
        public void Sort()
        {
            _bodyParts.Sort(InteractableBodyPart.CompareByPriority);
        }

        /// <summary>
        /// Gets enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return _bodyParts.GetEnumerator();
        }

        /// <summary>
        /// Clears this component
        /// </summary>
        public void Clear()
        {
            _rwLock.EnterWriteLock();
            _bodyParts.Clear();
            _rwLock.ExitWriteLock();

            this.Root = null;
        }

        /// <summary>
        /// 6
        /// </summary>
        /// <param name="msg"></param>
        public override void Pack(Lidgren.Network.NetBuffer msg)
        {
            // Write header
            msg.WriteRangedInteger(0, (Int32)(InteractableComponentType.Max), (Int32)InteractableComponentType.Appearance);

            // Write map appearance
            msg.Write(this.MapDir);
            msg.Write(this.AnimationFrame);

            // Write avatar appearance
            _rwLock.EnterReadLock();
            
                msg.Write(this.Count);
                foreach (InteractableBodyPart part in this)
                {
                    msg.Write((Byte)part.Type);
                    msg.Write(part.Opacity);
                    msg.Write(part.Hue);
                    msg.Write(part.Priority);

                    if (part.Type == BodyPart.Skin) // 0 value
                        msg.Write(part.ByteValue ?? (Byte)1); // 1 Byte
                    else if (BodyPart.Integer.HasFlag(part.Type))
                        msg.Write(part.IntegerValue ?? 2); // 1 Int32
                    else if (BodyPart.ByteArr.HasFlag(part.Type))
                        msg.Write(part.ArrayByteValue ?? new Byte[] { 2, 2 }); // 2 Bytes;
                    else if (BodyPart.Byte.HasFlag(part.Type))
                        msg.Write(part.ByteValue ?? (Byte)1); // 1 Byte
                    else if (BodyPart.String.HasFlag(part.Type))
                        msg.Write(part.StringValue ?? String.Empty); // 1 String
                    else
                    {
                        //ERAUtils.Logger.Logger.Warning("Bodypart has no value!");
                    }
                }

            _rwLock.ExitReadLock();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static InteractableAppearance Unpack(NetBuffer buffer)
        {
            var result = new InteractableAppearance();
            result.MapDir = buffer.ReadByte();
            result.AnimationFrame = buffer.ReadByte();

            var parts = buffer.ReadInt32();
            for(; parts > 0; parts--)
            {
                InteractableBodyPart part = null;

                var type = (BodyPart)buffer.ReadByte();
                var opacity = buffer.ReadByte();
                var hue = buffer.ReadByte();
                var priority = buffer.ReadByte();
                if (type == BodyPart.Skin)
                    part = InteractableBodyPart.Generate(type, buffer.ReadByte(), opacity, hue, priority);
                else if (BodyPart.Integer.HasFlag(part.Type))
                    part = InteractableBodyPart.Generate(type, buffer.ReadInt32(), opacity, hue, priority);
                else if (BodyPart.ByteArr.HasFlag(part.Type))
                    part = InteractableBodyPart.Generate(type, buffer.ReadBytes(2), opacity, hue, priority);
                else if (BodyPart.Byte.HasFlag(part.Type))
                    part = InteractableBodyPart.Generate(type, buffer.ReadByte(), opacity, hue, priority);
                else if (BodyPart.String.HasFlag(part.Type))
                    part = InteractableBodyPart.Generate(type, buffer.ReadString(), opacity, hue, priority);

                result.AddPart(part);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public InteractableBodyPart RemoveWeapon(Int32 id)
        {
            _rwLock.EnterWriteLock();
            InteractableBodyPart pending = _bodyParts.First(bp => bp.Type == BodyPart.Weapon && bp.IntegerValue == id);
            _bodyParts.Remove(pending);
            _rwLock.ExitWriteLock();

            return pending;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public InteractableBodyPart RemoveArmor(Int32 id)
        {
            _rwLock.EnterWriteLock();
            InteractableBodyPart pending = _bodyParts.First(bp => bp.Type == BodyPart.Armor && bp.IntegerValue == id);
            _bodyParts.Remove(pending);
            _rwLock.ExitWriteLock();

            return pending;
        }
    }
}
