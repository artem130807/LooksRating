#  Pyrofork - Telegram MTProto API Client Library for Python
#  Copyright (C) 2017-present Dan <https://github.com/delivrance>
#  Copyright (C) 2022-present Mayuri-Chan <https://github.com/Mayuri-Chan>
#
#  This file is part of Pyrofork.
#
#  Pyrofork is free software: you can redistribute it and/or modify
#  it under the terms of the GNU Lesser General Public License as published
#  by the Free Software Foundation, either version 3 of the License, or
#  (at your option) any later version.
#
#  Pyrofork is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU Lesser General Public License for more details.
#
#  You should have received a copy of the GNU Lesser General Public License
#  along with Pyrofork.  If not, see <http://www.gnu.org/licenses/>.

from io import BytesIO

from pyrogram.raw.core.primitives import Int, Long, Int128, Int256, Bool, Bytes, String, Double, Vector
from pyrogram.raw.core import TLObject
from pyrogram import raw
from typing import List, Optional, Any

# # # # # # # # # # # # # # # # # # # # # # # #
#               !!! WARNING !!!               #
#          This is a generated file!          #
# All changes made in this file will be lost! #
# # # # # # # # # # # # # # # # # # # # # # # #


class MessageActionStarGift(TLObject):  # type: ignore
    """Telegram API type.

    Constructor of :obj:`~pyrogram.raw.base.MessageAction`.

    Details:
        - Layer: ``189``
        - ID: ``9BB3EF44``

    Parameters:
        gift (:obj:`StarGift <pyrogram.raw.base.StarGift>`):
            N/A

        convert_stars (``int`` ``64-bit``):
            N/A

        name_hidden (``bool``, *optional*):
            N/A

        saved (``bool``, *optional*):
            N/A

        converted (``bool``, *optional*):
            N/A

        message (:obj:`TextWithEntities <pyrogram.raw.base.TextWithEntities>`, *optional*):
            N/A

    """

    __slots__: List[str] = ["gift", "convert_stars", "name_hidden", "saved", "converted", "message"]

    ID = 0x9bb3ef44
    QUALNAME = "types.MessageActionStarGift"

    def __init__(self, *, gift: "raw.base.StarGift", convert_stars: int, name_hidden: Optional[bool] = None, saved: Optional[bool] = None, converted: Optional[bool] = None, message: "raw.base.TextWithEntities" = None) -> None:
        self.gift = gift  # StarGift
        self.convert_stars = convert_stars  # long
        self.name_hidden = name_hidden  # flags.0?true
        self.saved = saved  # flags.2?true
        self.converted = converted  # flags.3?true
        self.message = message  # flags.1?TextWithEntities

    @staticmethod
    def read(b: BytesIO, *args: Any) -> "MessageActionStarGift":
        
        flags = Int.read(b)
        
        name_hidden = True if flags & (1 << 0) else False
        saved = True if flags & (1 << 2) else False
        converted = True if flags & (1 << 3) else False
        gift = TLObject.read(b)
        
        message = TLObject.read(b) if flags & (1 << 1) else None
        
        convert_stars = Long.read(b)
        
        return MessageActionStarGift(gift=gift, convert_stars=convert_stars, name_hidden=name_hidden, saved=saved, converted=converted, message=message)

    def write(self, *args) -> bytes:
        b = BytesIO()
        b.write(Int(self.ID, False))

        flags = 0
        flags |= (1 << 0) if self.name_hidden else 0
        flags |= (1 << 2) if self.saved else 0
        flags |= (1 << 3) if self.converted else 0
        flags |= (1 << 1) if self.message is not None else 0
        b.write(Int(flags))
        
        b.write(self.gift.write())
        
        if self.message is not None:
            b.write(self.message.write())
        
        b.write(Long(self.convert_stars))
        
        return b.getvalue()
