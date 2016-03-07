using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCamd
{
    enum Message
    {
        MSG_CLIENT_2_SERVER_LOGIN = 224,
        MSG_CLIENT_2_SERVER_LOGIN_ACK,
        MSG_CLIENT_2_SERVER_LOGIN_NAK,
        MSG_CARD_DATA_REQ,
        MSG_CARD_DATA,
        MSG_SERVER_2_CLIENT_NAME,
        MSG_SERVER_2_CLIENT_NAME_ACK,
        MSG_SERVER_2_CLIENT_NAME_NAK,
        MSG_SERVER_2_CLIENT_LOGIN,
        MSG_SERVER_2_CLIENT_LOGIN_ACK,
        MSG_SERVER_2_CLIENT_LOGIN_NAK,
        MSG_ADMIN,
        MSG_ADMIN_ACK,
        MSG_ADMIN_LOGIN,
        MSG_ADMIN_LOGIN_ACK,
        MSG_ADMIN_LOGIN_NAK,
        MSG_ADMIN_COMMAND,
        MSG_ADMIN_COMMAND_ACK,
        MSG_ADMIN_COMMAND_NAK,
        MSG_KEEPALIVE = 224 + 0x1d,
    }
    class NewCamdHandler
    {

    }
}
