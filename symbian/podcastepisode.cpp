/**
 * This file is part of Podcatcher for N9.
 * Author: Johan Paul (johan.paul@d-pointer.com)
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
#include <QNetworkRequest>
#include <QNetworkReply>
#include <QDir>
#include <QVariant>

#include <QtDebug>

#include "podcastepisode.h"
#include "podcastmanager.h"

#ifdef Q_OS_LINUX
    #include "podcastepisodemeego.h"
#endif

#ifdef Q_OS_SYMBIAN
    #include "podcastepisodesymbian.h"
#endif

PodcastEpisode::PodcastEpisode(QObject *parent) :
    QObject(parent),
    m_dlNetworkManager(0)
{
    m_state = PodcastEpisode::GetState;
    m_bytesDownloaded = 0;
    m_lastPlayed = QDateTime();
    m_hasBeenCanceled = false;
    m_currentDownload = 0;
    m_playFilename = "";
}

PodcastEpisode * PodcastEpisode::newEpisode()
{
    PodcastEpisode *episode = 0;

#ifdef Q_OS_LINUX
        // The Linux Podcast manager
        episode = new PodcastEpisodeMeego(NULL);
#endif

#ifdef Q_OS_SYMBIAN
        qDebug() << "Creating Symbian episode.";
        episode = new PodcastEpisodeSymbian(NULL);
#endif

    return episode;
}

void PodcastEpisode::setTitle(const QString &title)
{
    m_title = title;
}

QString PodcastEpisode::title() const
{
    return m_title;
}

void PodcastEpisode::setDownloadLink(const QString &downloadLink)
{
    m_downloadLink = downloadLink;
}

QString PodcastEpisode::downloadLink() const
{
    if (m_downloadLink.isEmpty()) {
        qWarning() << "Download link for postcast is empty! Cannot download.";
    }
    return m_downloadLink;
}

void PodcastEpisode::setDescription(const QString &desc)
{
    m_description = desc;
}

QString PodcastEpisode::description() const
{
    return m_description;
}

void PodcastEpisode::setPubTime(const QDateTime &pubDate)
{
    m_pubDateTime = pubDate;
}

QDateTime PodcastEpisode::pubTime() const
{
    return m_pubDateTime;
}

void PodcastEpisode::setDuration(const QString &duration)
{
    m_duration = duration;
}

QString PodcastEpisode::duration() const
{
    return m_duration;
}

void PodcastEpisode::setDownloadSize(qint64 downloadSize)
{
    m_downloadSize = downloadSize;
}

qint64 PodcastEpisode::downloadSize() const
{
    return m_downloadSize;
}

qint64 PodcastEpisode::alreadyDownloaded()
{
    return m_bytesDownloaded;
}

void PodcastEpisode::setDbId(int id)
{
    m_dbid = id;
}


int PodcastEpisode::dbid() const
{
    return m_dbid;
}

void PodcastEpisode::setState(PodcastEpisode::EpisodeStates newState)
{
    if (m_state != newState) {
        qDebug() << "Setting episode state to " << newState;
        m_state = newState;
        emit episodeChanged();
    }
}

QString PodcastEpisode::episodeState() const
{

    // Optimize: since downloading is asked several times during downloading, put it here first.
    if (m_state == DownloadingState) {
        return QString("downloading");
    }

    if (m_lastPlayed.isValid()) {
        return "played";
    }

    if (!m_playFilename.isEmpty()) {
        return "downloaded";
    }

    if (!m_hasBeenCanceled) {
        if (m_downloadLink.isEmpty()) {
            return "undownloadable";
        }
    }

    switch(m_state) {
    case DownloadedState:
        return QString("downloaded");
        break;
    case QueuedState:
        return QString("queued");
        break;
    case GetState:
    case CanceledState:
    default:
        return QString("get");
        break;
    }
}

void PodcastEpisode::setPlayFilename(const QString &playFilename)
{
    if (playFilename != m_playFilename) {
        m_playFilename = playFilename;
        emit episodeChanged();
    }
}

QString PodcastEpisode::playFilename() const
{
    return m_playFilename;
}

void PodcastEpisode::setChannelId(int id)
{
    m_channelid = id;
}

int PodcastEpisode::channelid() const
{
    return m_channelid;
}


void PodcastEpisode::setDownloadManager(QNetworkAccessManager *qnam)
{
    m_dlNetworkManager = qnam;
}

void PodcastEpisode::setLastPlayed(const QDateTime &lastPlayed)
{
    if (lastPlayed != m_lastPlayed) {
        m_lastPlayed = lastPlayed;
        emit episodeChanged();
    }
}

QDateTime PodcastEpisode::lastPlayed() const
{
    return m_lastPlayed;
}

void PodcastEpisode::setHasBeenCanceled(bool canceled)
{
    canceled ? m_state = PodcastEpisode::CanceledState : m_state = m_state;

    if (canceled != m_hasBeenCanceled) {
        m_hasBeenCanceled = canceled;
        emit episodeChanged();
    }

}

bool PodcastEpisode::hasBeenCanceled() const
{
    return m_hasBeenCanceled;
}

void PodcastEpisode::cancelCurrentDownload()
{
    if (m_currentDownload != 0 &&
        m_state == DownloadingState) {
        qDebug() << "Canceling current episode download request...";

        setHasBeenCanceled(true);

        // Abort current download.
        disconnect(m_currentDownload, SIGNAL(finished()),
                this, SLOT(onPodcastEpisodeDownloadCompleted()));
        disconnect(m_currentDownload, SIGNAL(downloadProgress(qint64,qint64)),
                this, SLOT(onDownloadProgress(qint64, qint64)));
        m_currentDownload->abort();

    }
}

void PodcastEpisode::deleteDownload()
{
    if (m_playFilename.isEmpty()) {
        return;
    } else {
        qDebug() << "Deleting locally downloaded podcast:" << m_playFilename;
    }

    QFile download(m_playFilename);
    if (!download.remove()) {
        QFileInfo fi(download);
        qWarning() << "Unable to remove locally downloaded podcast:" << fi.canonicalFilePath();
    }

    cancelCurrentDownload();
    setPlayFilename("");
    setLastPlayed(QDateTime());
    setState(PodcastEpisode::GetState);
    setHasBeenCanceled(true);             // TODO: This will denote to the UI not to download it again automatically. Better method name would be good.
}

void PodcastEpisode::setAsPlayed()
{
    setLastPlayed(QDateTime::currentDateTime());
}

/*
bool PodcastEpisode::isValidAudiofile() const
{
    qDebug() << "DOWNLOAD LINK:" << m_downloadLink;
    if (m_downloadLink.isEmpty()) {
        return false;
    }

    // Download file must be some of these.
    if (m_downloadLink.endsWith(".mp3", Qt::CaseInsensitive) ||
        m_downloadLink.endsWith(".mp4", Qt::CaseInsensitive) ||
        m_downloadLink.endsWith(".ogg", Qt::CaseInsensitive) ||
        m_downloadLink.endsWith(".wav", Qt::CaseInsensitive) ) {
        return true;
    }

    return false;
}
*/

/*bool PodcastEpisode::isOnlyWebsiteUrl() const
{
    return (!isValidAudiofile() && QUrl(m_downloadLink).isValid());
}
*/
void PodcastEpisode::getAudioUrl()
{
    m_streamResolverTries = 0;
    m_streamResolverManager = new QNetworkAccessManager(this);
    QNetworkRequest request;
    request.setUrl(this->downloadLink());

    QNetworkReply *reply = m_streamResolverManager->get(request);
    connect(reply, SIGNAL(metaDataChanged()),
            this,  SLOT(onAudioUrlMetadataChanged()));

}

void PodcastEpisode::onAudioUrlMetadataChanged()
{
    bool audioFound = false;
    QNetworkReply *reply = qobject_cast<QNetworkReply *>(sender());

    if (m_streamResolverTries >= 5) {
        qDebug() << "Did not find a proper MP3 URL to stream! Giving up after " << m_streamResolverTries << " tries.";
        emit streamingUrlResolved("", "");
        m_streamResolverManager->deleteLater();
    }

    QString contentType = reply->header(QNetworkRequest::ContentTypeHeader).toString();

    if (contentType == "audio/mpeg" ||
        contentType == "audio/mpeg3") {
        qDebug() << "Found MP3 URL to stream: " << reply->url();
        emit streamingUrlResolved(reply->url().toString(), m_title);
        m_streamResolverManager->deleteLater();
        audioFound = true;
    }

    QString redirectedUrl = PodcastManager::redirectedRequest(reply);
    if (QUrl(redirectedUrl).isValid()) {
        qDebug() << "We have been redirected...";
        QNetworkRequest request;
        request.setUrl(redirectedUrl);

        QNetworkReply *newReply = m_streamResolverManager->get(request);
        connect(newReply, SIGNAL(metaDataChanged()),
                this,  SLOT(onAudioUrlMetadataChanged()));

        m_streamResolverTries++;

    } else if (!audioFound) {
        qDebug() << "Error resolving streaming URL!";
        emit streamingUrlResolved("", "");
        m_streamResolverManager->deleteLater();
    }

    reply->deleteLater();
}
