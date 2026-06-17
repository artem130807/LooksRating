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


class UserStarGift(TLObject):  # type: ignore
    """Telegram API type.

    Constructor of :obj:`~pyrogram.raw.base.UserStarGift`.

    Details:
        - Layer: ``189``
        - ID: ``EEA49A6E``

    Parameters:
        date (``int`` ``32-bit``):
            N/A

        gift (:obj:`StarGift <pyrogram.raw.base.StarGift>`):
            N/A

        name_hidden (``bool``, *optional*):
            N/A

        unsaved (``bool``, *optional*):
            N/A

        from_id (``int`` ``64-bit``, *optional*):
            N/A

        message (:obj:`TextWithEntities <pyrogram.raw.base.TextWithEntities>`, *optional*):
            N/A

        msg_id (``int`` ``32-bit``, *optional*):
            N/A

        convert_stars (``int`` ``64-bit``, *optional*):
            N/A

    """

    __slots__: List[str] = ["date", "gift", "name_hidden", "unsaved", "from_id", "message", "msg_id", "convert_stars"]

    ID = 0xeea49a6e
    QUALNAME = "types.UserStarGift"

    def __init__(self, *, date: int, gift: "raw.base.StarGift", name_hidden: Optional[bool] = None, unsaved: Optional[bool] = None, from_id: Optional[int] = None, message: "raw.base.TextWithEntities" = None, msg_id: Optional[int] = None, convert_stars: Optional[int] = None) -> None:
        self.date = date  # int
        self.gift = gift  # StarGift
        self.name_hidden = name_hidden  # flags.0?true
        self.unsaved = unsaved  # flags.5?true
        self.from_id = from_id  # flags.1?long
        self.message = message  # flags.2?TextWithEntities
        self.msg_id = msg_id  # flags.3?int
        self.convert_stars = convert_stars  # flags.4?long

    @staticmethod
    def read(b: BytesIO, *args: Any) -> "UserStarGift":
        
        flags = Int.read(b)
        
        name_hidden = True if flags & (1 << 0) else False
        unsaved = True if flags & (1 << 5) else False
        from_id = Long.read(b) if flags & (1 << 1) else None
        date = Int.read(b)
        
        gift = TLObject.read(b)
        
        message = TLObject.read(b) if flags & (1 << 2) else None
        
        msg_id = Int.read(b) if flags & (1 << 3) else None
        convert_stars = Long.read(b) if flags & (1 << 4) else None
        return UserStarGift(date=date, gift=gift, name_hidden=name_hidden, unsaved=unsaved, from_id=from_id, message=message, msg_id=msg_id, convert_stars=convert_stars)

    def write(self, *args) -> bytes:
        b = BytesIO()
        b.write(Int(self.ID, False))

        flags = 0
        flags |= (1 << 0) if self.name_hidden else 0
        flags |= (1 << 5) if self.unsaved else 0
        flags |= (1 << 1) if self.from_id is not None else 0
        flags |= (1 << 2) if self.message is not None else 0
        flags |= (1 << 3) if self.msg_id is not None else 0
        flags |= (1 << 4) if self.convert_stars is not None else 0
        b.write(Int(flags))
        
        if self.from_id is not None:
            b.write(Long(self.from_id))
        
        b.write(Int(self.date))
        
        b.write(self.gift.write())
        
        if self.message is not None:
            b.write(self.message.write())
        
        if self.msg_id is not None:
            b.write(Int(self.msg_id))
        
        if self.convert_stars is not None:
            b.write(Long(self.convert_stars))
        
        return b.getvalue()
