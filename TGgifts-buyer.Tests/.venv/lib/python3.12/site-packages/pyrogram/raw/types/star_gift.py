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


class StarGift(TLObject):  # type: ignore
    """Telegram API type.

    Constructor of :obj:`~pyrogram.raw.base.StarGift`.

    Details:
        - Layer: ``189``
        - ID: ``AEA174EE``

    Parameters:
        id (``int`` ``64-bit``):
            N/A

        sticker (:obj:`Document <pyrogram.raw.base.Document>`):
            N/A

        stars (``int`` ``64-bit``):
            N/A

        convert_stars (``int`` ``64-bit``):
            N/A

        limited (``bool``, *optional*):
            N/A

        availability_remains (``int`` ``32-bit``, *optional*):
            N/A

        availability_total (``int`` ``32-bit``, *optional*):
            N/A

    """

    __slots__: List[str] = ["id", "sticker", "stars", "convert_stars", "limited", "availability_remains", "availability_total"]

    ID = 0xaea174ee
    QUALNAME = "types.StarGift"

    def __init__(self, *, id: int, sticker: "raw.base.Document", stars: int, convert_stars: int, limited: Optional[bool] = None, availability_remains: Optional[int] = None, availability_total: Optional[int] = None) -> None:
        self.id = id  # long
        self.sticker = sticker  # Document
        self.stars = stars  # long
        self.convert_stars = convert_stars  # long
        self.limited = limited  # flags.0?true
        self.availability_remains = availability_remains  # flags.0?int
        self.availability_total = availability_total  # flags.0?int

    @staticmethod
    def read(b: BytesIO, *args: Any) -> "StarGift":
        
        flags = Int.read(b)
        
        limited = True if flags & (1 << 0) else False
        id = Long.read(b)
        
        sticker = TLObject.read(b)
        
        stars = Long.read(b)
        
        availability_remains = Int.read(b) if flags & (1 << 0) else None
        availability_total = Int.read(b) if flags & (1 << 0) else None
        convert_stars = Long.read(b)
        
        return StarGift(id=id, sticker=sticker, stars=stars, convert_stars=convert_stars, limited=limited, availability_remains=availability_remains, availability_total=availability_total)

    def write(self, *args) -> bytes:
        b = BytesIO()
        b.write(Int(self.ID, False))

        flags = 0
        flags |= (1 << 0) if self.limited else 0
        flags |= (1 << 0) if self.availability_remains is not None else 0
        flags |= (1 << 0) if self.availability_total is not None else 0
        b.write(Int(flags))
        
        b.write(Long(self.id))
        
        b.write(self.sticker.write())
        
        b.write(Long(self.stars))
        
        if self.availability_remains is not None:
            b.write(Int(self.availability_remains))
        
        if self.availability_total is not None:
            b.write(Int(self.availability_total))
        
        b.write(Long(self.convert_stars))
        
        return b.getvalue()
