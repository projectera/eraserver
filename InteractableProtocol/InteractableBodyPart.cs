using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace InteractableProtocol
{
    [Serializable]
    public class InteractableBodyPart // Struct wouldn't properties deserialize...
    {
        /// <summary>
        /// Data Type
        /// </summary>
        [BsonRequired]
        public BodyPart Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Data value for this body part
        /// </summary>
        internal Object Value
        {
            private get;
            set;
        }

        /// <summary>
        /// Parts opacity
        /// </summary>
        [BsonRequired]
        public Byte Opacity
        {
            get;
            set;
        }

        /// <summary>
        /// Parts hue
        /// </summary>
        public Byte Hue
        {
            get;
            set;
        }

        /// <summary>
        /// Parts priority
        /// </summary>
        [BsonRequired]
        public Byte Priority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets data value as byte
        /// </summary>
        [BsonIgnoreIfNull]
        public Byte? ByteValue
        {
            get
            {
                if (Value is Byte)
                    return (Byte)Value;
                return null;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        /// Gets/Sets data value as String
        /// </summary>
        [BsonIgnoreIfNull]
        public String StringValue
        {
            get
            {
                if (Value is String)
                    return (String)Value;
                return null;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        /// Gets/Sets data value as tuple of bytes
        /// </summary>
        [BsonIgnoreIfNull]
        public Byte[] ArrayByteValue
        {
            get
            {
                if (Value is Byte[])
                    return (Byte[])Value;
                return null;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        /// Gets/Sets data value as Integer
        /// </summary>
        [BsonIgnoreIfNull]
        public Int32? IntegerValue
        {
            get
            {
                if (Value is Int32)
                    return (Int32)Value;
                return null;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        /// Gets data value as first tuple item
        /// </summary>
        internal Byte TupleLeftValue
        {
            get
            {
                return ArrayByteValue[0];
            }
        }

        /// <summary>
        /// Gets data value as second tuple item
        /// </summary>
        internal Byte TupleRightValue
        {
            get
            {
                return ArrayByteValue[1];
            }
        }

        /// <summary>
        /// Generates a new body part
        /// </summary>
        /// <param name="type">BodyPart type</param>
        /// <param name="value">data</param>
        /// <param name="hue">hue</param>
        /// <param name="opacity">opacity</param>
        /// <param name="priority">priority</param>
        /// <returns></returns>
        public static InteractableBodyPart Generate(BodyPart type, Object value, Byte opacity, Byte hue, Byte priority)
        {
            if (!(value is Byte || value is Byte[] || value is Int32 | value is String))
                throw new ArgumentException("value", "The type of value is not supported.");
            if (!ValidateTypeAndValue(type, value))
                throw new ArgumentException("value", "The type of value is in the wrong format.");

            InteractableBodyPart result = new InteractableBodyPart();
            result.Type = type;
            result.Value = value;
            result.Opacity = opacity;
            result.Hue = hue;
            result.Priority = priority;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static Boolean ValidateTypeAndValue(BodyPart type, Object value)
        {
            if (value is Byte)
                return BodyPart.Byte.HasFlag(type);
            if (value is Byte[])
                return BodyPart.ByteArr.HasFlag(type);
            if (value is Int32)
                return BodyPart.Integer.HasFlag(type);
            if (value is String)
                return BodyPart.String.HasFlag(type);

            return false;
        }

        /// <summary>
        /// Generates a skin with skintone and default values
        /// </summary>
        /// <param name="skintone"></param>
        /// <returns></returns>
        public static InteractableBodyPart GenerateSkin(Byte skintone)
        {
            return Generate(BodyPart.Skin, skintone, 255, 0, 10);
        }

        /// <summary>
        /// Generates eyes with eye type, color and default values
        /// </summary>
        /// <param name="eyeType"></param>
        /// <param name="eyeColor"></param>
        /// <returns></returns>
        public static InteractableBodyPart GenerateEyes(Byte eyeType, Byte eyeColor)
        {
            return Generate(BodyPart.Eyes, new Byte[] { eyeType, eyeColor }, 255, 0, 20);
        }

        /// <summary>
        /// Generates hair with hair type, color and default values
        /// </summary>
        /// <param name="hairType"></param>
        /// <param name="hairColor"></param>
        /// <returns></returns>
        public static InteractableBodyPart GenerateHair(Byte hairType, Byte hairColor)
        {
            return Generate(BodyPart.Hair, new Byte[] { hairType, hairColor }, 255, 0, 30);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weaponAssetId"></param>
        /// <returns></returns>
        public static InteractableBodyPart GenerateWeapon(Int32 weaponAssetId)
        {
            return Generate(BodyPart.Weapon, weaponAssetId, 255, 0, 225);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weaponArmorId"></param>
        /// <returns></returns>
        public static InteractableBodyPart GenerateArmor(Int32 armorAssetId, EquipmentPart part)
        {
            Byte priority = 125;
            switch (part)
            {
                case EquipmentPart.None:
                    priority = 0;
                    break;
                case EquipmentPart.Head:
                    priority = 250;
                    break;
                case EquipmentPart.Over:
                    priority = 199;
                    break;
                case EquipmentPart.Top:
                    priority = 175;
                    break;
                case EquipmentPart.Hands:
                case EquipmentPart.Neck:
                    priority = 150;
                    break;
                case EquipmentPart.ArmLeft:
                case EquipmentPart.ArmRight:
                case EquipmentPart.Feet:
                    priority = 125;
                    break;
                case EquipmentPart.Bottom:
                    priority = 100;
                    break;
                case EquipmentPart.KeyItem:
                case EquipmentPart.Extra:
                    priority = 50;
                    break;
            }
            return Generate(BodyPart.Armor, armorAssetId, 255, 0, priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="armorAssetId"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static InteractableBodyPart GenerateArmor(Int32 armorAssetId, Byte priority)
        {
            return Generate(BodyPart.Armor, armorAssetId, 255, 0, priority);
        }


        /// <summary>
        /// Compares two interactableBodyParts by priority (and if equal by type)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Int32 CompareByPriority(InteractableBodyPart x, InteractableBodyPart y)
        {
            Int32 retval = x.Priority.CompareTo(y.Priority);
            if (retval == 0)
                return x.Type.CompareTo(y.Type);
            return retval;
        }
    }
}