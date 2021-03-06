/**
 * This file is part of Podcatcher for Sailfish OS.
 * Authors: Johan Paul (johan.paul@gmail.com)
 *          Moritz Carmesin (carolus@carmesinus.de)
 *
 * Podcatcher for Sailfish OS is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Podcatcher for Sailfish OS is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Podcatcher for Sailfish OS.  If not, see <http://www.gnu.org/licenses/>.
 */
import QtQuick 2.0
import Sailfish.Silica 1.0


Item {
//Column{
    width: parent.width
    height: Math.max(channelLogo.height, channelDescription.height) + Theme.paddingMedium + autoDownloadSwitch.height

    PodcastChannelLogo {
            id: channelLogo;
            anchors.left: parent.left
            anchors.leftMargin: 0
            channelLogo: channel.logo
            width: Theme.itemSizeHuge
            height: Theme.itemSizeHuge

        }

        Label {
            id: channelDescription
            //height: channelLogo.height
            width: parent.width- channelLogo.width - Theme.paddingMedium - Theme.horizontalPageMargin
            anchors.left: channelLogo.right
            anchors.leftMargin:  Theme.paddingMedium
            text: channel.description
            truncationMode: TruncationMode.None
            font.pixelSize: Theme.fontSizeSmall
            wrapMode: Text.WordWrap
        }
//    }

    TextSwitch{
        id: autoDownloadSwitch

        anchors.top: (channelLogo.height>channelDescription.height)?channelLogo.bottom:channelDescription.bottom
        anchors.topMargin: Theme.paddingSmall

        width: parent.width


        text: qsTr("Auto-download")
        checked: channel.isAutoDownloadOn

        onCheckedChanged: {
            appWindow.autoDownloadChanged(channel.channelId, checked);
        }
    }

//}
}
