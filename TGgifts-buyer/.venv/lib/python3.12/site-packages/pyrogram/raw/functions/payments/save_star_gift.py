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


class SaveStarGift(TLObject):  # type: ignore
    """Telegram API function.

    Details:
        - Layer: ``189``
        - ID: ``87ACF08E``

    Parameters:
        user_id (:obj:`InputUser <pyrogram.raw.base.InputUser>`):
            N/A

        msg_id (``int`` ``32-bit``):
            N/A

        unsave (``bool``, *optional*):
            N/A

    Returns:
        ``bool``
    """

    __slots__: List[str] = ["user_id", "msg_id", "unsave"]

    ID = 0x87acf08e
    QUALNAME = "functions.payments.SaveStarGift"

    def __init__(self, *, user_id: "raw.base.InputUser", msg_id: int, unsave: Optional[bool] = None) -> None:
        self.user_id = user_id  # InputUser
        self.msg_id = msg_id  # int
        self.unsave = unsave  # flags.0?true

    @staticmethod
    def read(b: BytesIO, *args: Any) -> "SaveStarGift":
        
        flags = Int.read(b)
        
        unsave = True if flags & (1 << 0) else False
        user_id = TLObject.read(b)
        
        msg_id = Int.read(b)
        
        return SaveStarGift(user_id=user_id, msg_id=msg_id, unsave=unsave)

    def write(self, *args) -> bytes:
        b = BytesIO()
        b.write(Int(self.ID, False))

        flags = 0
        flags |= (1 << 0) if self.unsave else 0
        b.write(Int(flags))
        
        b.write(self.user_id.write())
        
        b.write(Int(self.msg_id))
        
        return b.getvalue()
