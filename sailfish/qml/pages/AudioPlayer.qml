/**
 * This file is part of Podcatcher for N9.
 * Author: Johan Paul (johan.paul@gmail.com)
 *
 * Podcatcher for N9 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Podcatcher for N9 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Podcatcher for N9.  If not, see <http://www.gnu.org/licenses/>.
 */
import QtQuick 2.2
import QtMultimedia 5.0

import Sailfish.Silica 1.0
import  org.nemomobile.configuration 1.0

import org.nemomobile.mpris 1.0

Page {
    id: playerPage

    signal playStream(string url, string title)
    signal playFile(string fileName)
    signal stopStream(string url)

    property string title : mediaMetaDataExtractor.title
    property bool playing : audioPlayer.playbackState === Audio.PlayingState
    property bool paused: audioPlayer.playbackState === Audio.PausedState
    property bool active: paused || playing


    ConfigurationValue{
        id: lastPosition
        key: ""
        defaultValue: 0
    }

    function durationText(curPos) {
        var curPosSecs = Math.floor((parseInt(curPos) / 1000));

        var curPlayTimeSecs = (curPosSecs % 60);
        var curPlayTimeMins = Math.floor((curPosSecs / 60));
        var curPlayTimeHours = Math.floor((curPosSecs / 3600));

        if (curPlayTimeHours == 0) {
            curPlayTimeHours = ""
        } else if (curPlayTimeHours < 10) {
            curPlayTimeHours = "0" + curPlayTimeHours.toString();
        }

        if (curPlayTimeHours > 0) {
            curPlayTimeMins = curPlayTimeMins % 60;
        }

        if (curPlayTimeSecs < 10) {
            curPlayTimeSecs = "0" + curPlayTimeSecs;
        }

        if (curPlayTimeMins < 10) {
            curPlayTimeMins = "0" + curPlayTimeMins;
        }

        if (curPlayTimeHours.toString().length > 0) {
            curPlayTimeHours = curPlayTimeHours + " : ";
        }

        var playtimeString = curPlayTimeHours.toString() + curPlayTimeMins.toString() + " : " + curPlayTimeSecs.toString();

        return playtimeString;
    }

    PageHeader{
        id: header
        title: qsTr("Player")
    }


    Column {
        id: buttonGroup
        spacing: Theme.paddingMedium
        width: parent.width
        anchors.top: header.bottom
        anchors.bottom: parent.bottom
        anchors.margins: Theme.paddingMedium
        anchors.bottomMargin: Theme.paddingLarge

        Image {
            id: cover

            width: parent.width+2*Theme.paddingMedium
            height: width
            anchors.horizontalCenter: parent.horizontalCenter
            visible: true
            cache: false
        }


        Label {
            id: titleLabel
            text: title
            width: parent.width //- durationLabel.width
            height: Text.paintedHeight
            anchors.horizontalCenter: parent.horizontalCenter
            //elide: Text.ElideRight
            horizontalAlignment: Text.AlignHCenter
            font.pixelSize: Theme.fontSizeSmall
        }


        Slider{

            property int position: audioPlayer.position

            id: playbackPosition
            width: parent.width
            //height: Theme.iconSizeMedium
            minimumValue: 0
            maximumValue: audioPlayer.duration
            value: audioPlayer.position
            enabled: audioPlayer.seekable
            handleVisible: true


            onPositionChanged: {
                if (!pressed) {
                    value = position
                }
            }
            onReleased: {
                if (audioPlayer.seekable) {
                    audioPlayer.seek(value);
                }
            }
        }

        Row {
            spacing: Theme.paddingSmall
            anchors.horizontalCenter: parent.horizontalCenter

            IconButton {
                id: rewBtn
                icon.source: "image://theme/icon-m-left?" + (pressed
                                                             ? Theme.highlightColor
                                                             : Theme.primaryColor)
                onClicked: {
                    console.log("Setting audio position to " + audioPlayer.position - 10000 + "s")
                    audioPlayer.seek(audioPlayer.position - 10000);
                }

                enabled: audioPlayer.playbackState == Audio.PlayingState ||
                         audioPlayer.playbackState == Audio.PausedState
            }


            IconButton {
                id: playBtn
                icon.source: (audioPlayer.playbackState !== Audio.PlayingState)?
                                 ("image://theme/icon-m-play?" + (pressed
                                                                  ? Theme.highlightColor
                                                                  : Theme.primaryColor)):
                                 ("image://theme/icon-m-pause?" + (pressed
                                                                   ? Theme.highlightColor
                                                                   : Theme.primaryColor))

                onClicked: {
                    if (audioPlayer.playbackState !== Audio.PlayingState)
                        audioPlayer.play();
                    else
                        audioPlayer.pause();
                }
            }




            IconButton {
                id:ffBtn
                icon.source: "image://theme/icon-m-right?" + (pressed
                                                              ? Theme.highlightColor
                                                              : Theme.primaryColor)

                enabled: audioPlayer.playbackState === Audio.PlayingState ||
                         audioPlayer.playbackState === Audio.PausedState


                onClicked: {
                    console.log("Setting audio position to " + audioPlayer.position + 10000 + "s")
                    //audioPlayer.position = audioPlayer.position + 10000
                    audioPlayer.seek(audioPlayer.position + 10000);
                }
            }

            IconButton{
                id: chapBtn

                icon.source: "image://theme/icon-m-events?" + (pressed
                                                              ? Theme.highlightColor
                                                              : Theme.primaryColor)

                onClicked: {
                    chapterPanel.open?chapterPanel.hide():chapterPanel.show();
                    console.log(channelsModel.columnCount())
                }

                visible: chapterModel.rowCount() > 0;
            }


        }

        Label {
            id: durationLabel
            text: durationText(audioPlayer.position) + "/" + durationText(audioPlayer.duration);
            font.pixelSize: Theme.fontSizeTiny
            anchors.horizontalCenter: parent.horizontalCenter
        }
    }


    DockedPanel{
        id:chapterPanel

//        dock: Dock.Right

//        width: parent.width/2
//        height: cover.height
//        anchors.top: cover.top

        dock: Dock.Top
        width: parent.width
        height: cover.height + header.height + Theme.paddingMedium

        open: false
        clip: true

        Rectangle{
            anchors.fill: parent

            //color: Theme.highlightBackgroundColor
            color: Theme.overlayBackgroundColor

        }


        SilicaListView{
            id: chapterList
            model: chapterModel

            anchors.fill: parent

            header: Label{
                text: qsTr("Chapters")
                font.family: Theme.fontFamilyHeading
                font.pixelSize: Theme.fontSizeLarge
                anchors.leftMargin: Theme.horizontalPageMargin
                anchors.rightMargin: Theme.horizontalPageMargin
                anchors.left: parent.left
                anchors.right: parent.right
            }


            delegate: ListItem{
                id: chapterItem
                width: parent.width
                contentHeight: chapTitleLabel.height +Theme.paddingMedium
                Label{
                    id: chapTitleLabel
                    anchors.leftMargin: Theme.horizontalPageMargin
                    anchors.rightMargin: Theme.horizontalPageMargin
                    anchors.verticalCenter: parent.verticalCenter
                    anchors.left: parent.left
                    anchors.right: parent.right
                    text: title
                    color: chapterItem.highlighted ? Theme.highlightColor : Theme.primaryColor
                    height: Text.paintedHeight
                    wrapMode: Text.WordWrap
                }

                onClicked: audioPlayer.seek(start)
            }

        }
    }


    onPlayStream: {
        audioPlayer.source = url;
        playerPage.title = title;
        audioPlayer.play();
    }


    onPlayFile: {
        var hash = Qt.md5(fileName);
        lastPosition.key = "/apps/ControlPanel/Podcatcher/position/"+hash;
        console.log("Last position key: "+lastPosition.key)

        audioPlayer.source = fileName;
        audioPlayer.seek(lastPosition.value);
        audioPlayer.play()
    }


    Audio {
        id: audioPlayer

        onPlaying: {
            console.log("Ok, we started playing...");
            console.log("Current position: "+position)
            //            for (var m in audioPlayer.metaData){
            //                console.log(m+': '+audioPlayer.metaData[m]);
            //            }

            console.log("Chapters count: "+chapterModel.columnCount());
        }

        onPaused: {
            lastPosition.value = position
            console.log("remembering position: " + position)
        }

        onStopped: {
//            lastPosition.value = position
//            console.log("remembering position: " + position)
        }

        onSourceChanged: {
            mediaMetaDataExtractor.inspect(source);
            console.log("Last position key: "+lastPosition.key);
            console.log("Last position: "+lastPosition.value);
            cover.source = '';
            cover.source = 'image://coverArt/cover';
        }
    }


}
