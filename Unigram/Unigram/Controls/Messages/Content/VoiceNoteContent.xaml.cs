﻿using System;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Services;
using Unigram.ViewModels;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Unigram.Controls.Messages.Content
{
    public sealed class VoiceNoteContent : Control, IContentWithFile
    {
        private MessageViewModel _message;
        public MessageViewModel Message => _message;

        private string _fileToken;

        public VoiceNoteContent(MessageViewModel message)
        {
            _message = message;

            DefaultStyleKey = typeof(VoiceNoteContent);

            Unloaded += OnUnloaded;
        }

        public VoiceNoteContent()
        {
            DefaultStyleKey = typeof(VoiceNoteContent);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            message.PlaybackService.PropertyChanged -= OnCurrentItemChanged;
            message.PlaybackService.PlaybackStateChanged -= OnPlaybackStateChanged;
            message.PlaybackService.PositionChanged -= OnPositionChanged;
        }

        #region InitializeComponent

        private FileButton Button;
        private ProgressVoice Progress;
        private TextBlock Subtitle;
        private ToggleButton Recognize;
        private TextBlock RecognizedText;
        private bool _templateApplied;

        protected override void OnApplyTemplate()
        {
            Button = GetTemplateChild(nameof(Button)) as FileButton;
            Progress = GetTemplateChild(nameof(Progress)) as ProgressVoice;
            Subtitle = GetTemplateChild(nameof(Subtitle)) as TextBlock;

            Button.Click += Button_Click;

            _templateApplied = true;

            if (_message != null)
            {
                UpdateMessage(_message);
            }
        }

        #endregion

        public void UpdateMessage(MessageViewModel message)
        {
            _message = message;

            message.PlaybackService.PropertyChanged -= OnCurrentItemChanged;

            var voiceNote = GetContent(message.Content);
            if (voiceNote == null || !_templateApplied)
            {
                return;
            }

            message.PlaybackService.PropertyChanged += OnCurrentItemChanged;

            Progress.UpdateWaveform(voiceNote);

            if (message.ClientService.IsPremium && message.SchedulingState == null)
            {
                if (Recognize == null)
                {
                    Recognize = GetTemplateChild(nameof(Recognize)) as ToggleButton;
                    Recognize.Click += Recognize_Click;
                }

                Recognize.Visibility = Visibility.Visible;
            }
            else if (Recognize != null)
            {
                Recognize.Visibility = Visibility.Collapsed;
            }

            UpdateRecognitionResult(voiceNote.SpeechRecognitionResult);

            UpdateManager.Subscribe(this, message, voiceNote.Voice, ref _fileToken, UpdateFile);
            UpdateFile(message, voiceNote.Voice);
        }

        private void UpdateRecognitionResult(SpeechRecognitionResult result)
        {
            if (result != null)
            {
                if (RecognizedText == null)
                {
                    RecognizedText = GetTemplateChild(nameof(RecognizedText)) as TextBlock;
                }

                if (result is SpeechRecognitionResultError)
                {
                    RecognizedText.Style = App.Current.Resources["InfoCaptionTextBlockStyle"] as Style;
                    RecognizedText.Text = Strings.Resources.NoWordsRecognized;
                }
                else if (result is SpeechRecognitionResultPending pending)
                {
                    // Add the loading thingy
                    RecognizedText.Style = App.Current.Resources["BodyTextBlockStyle"] as Style;
                    RecognizedText.Text = pending.PartialText;
                }
                else if (result is SpeechRecognitionResultText text)
                {
                    RecognizedText.Style = App.Current.Resources["BodyTextBlockStyle"] as Style;
                    RecognizedText.Text = text.Text;
                }

                RecognizedText.Visibility = Visibility.Visible;
            }
            else if (RecognizedText != null)
            {
                RecognizedText.Visibility = Visibility.Collapsed;
            }
        }

        public void Mockup(MessageVoiceNote voiceNote)
        {
            Progress.UpdateWaveform(voiceNote.VoiceNote);
            Progress.Minimum = 0;
            Progress.Maximum = 1;
            Progress.Value = 0.3;

            Subtitle.Text = FormatTime(TimeSpan.FromSeconds(1)) + " / " + FormatTime(TimeSpan.FromSeconds(3));

            Button.SetGlyph(0, MessageContentState.Pause);
        }

        #region Playback

        private void OnCurrentItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var voiceNote = GetContent(_message?.Content);
            if (voiceNote == null)
            {
                return;
            }

            this.BeginOnUIThread(() => UpdateFile(_message, voiceNote.Voice));
        }

        private void OnPlaybackStateChanged(IPlaybackService sender, object args)
        {
            var voiceNote = GetContent(_message?.Content);
            if (voiceNote == null)
            {
                return;
            }

            this.BeginOnUIThread(() => UpdateFile(_message, voiceNote.Voice));
        }

        private void OnPositionChanged(IPlaybackService sender, object args)
        {
            var position = sender.Position;
            var duration = sender.Duration;

            this.BeginOnUIThread(() => UpdatePosition(position, duration));
        }

        private void UpdateDuration()
        {
            var message = _message;
            if (message == null || !_templateApplied)
            {
                return;
            }

            var voiceNote = GetContent(message.Content);
            if (voiceNote == null)
            {
                return;
            }

            if (message.Content is MessageVoiceNote voiceNoteMessage)
            {
                Subtitle.Text = voiceNote.GetDuration() + (voiceNoteMessage.IsListened ? string.Empty : " ●");
                Progress.Maximum = voiceNote.Duration;
                Progress.Value = message.IsOutgoing || voiceNoteMessage.IsListened ? 0 : voiceNote.Duration;
            }
            else
            {
                Subtitle.Text = voiceNote.GetDuration();
                Progress.Maximum = voiceNote.Duration;
                Progress.Value = 0;
            }
        }

        private void UpdatePosition(TimeSpan position, TimeSpan duration)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            if (message.AreTheSame(message.PlaybackService.CurrentItem) /*&& !_pressed*/)
            {
                Subtitle.Text = FormatTime(position) + " / " + FormatTime(duration);
                Progress.Maximum = /*Slider.Maximum =*/ duration.TotalMilliseconds;
                Progress.Value = /*Slider.Value =*/ position.TotalMilliseconds;
            }
        }

        private string FormatTime(TimeSpan span)
        {
            if (span.TotalHours >= 1)
            {
                return span.ToString("h\\:mm\\:ss");
            }
            else
            {
                return span.ToString("mm\\:ss");
            }
        }

        #endregion

        public void UpdateMessageContentOpened(MessageViewModel message)
        {
            UpdateDuration();
        }

        private void UpdateFile(object target, File file)
        {
            UpdateFile(_message, file);
        }

        private void UpdateFile(MessageViewModel message, File file)
        {
            message.PlaybackService.PlaybackStateChanged -= OnPlaybackStateChanged;
            message.PlaybackService.PositionChanged -= OnPositionChanged;

            var voiceNote = GetContent(message.Content);
            if (voiceNote == null || !_templateApplied)
            {
                return;
            }

            else if (voiceNote.Voice.Id != file.Id)
            {
                return;
            }

            var size = Math.Max(file.Size, file.ExpectedSize);
            if (file.Local.IsDownloadingActive)
            {
                //Button.Glyph = Icons.Cancel;
                Button.SetGlyph(file.Id, MessageContentState.Downloading);
                Button.Progress = (double)file.Local.DownloadedSize / size;
            }
            else if (file.Remote.IsUploadingActive || message.SendingState is MessageSendingStateFailed)
            {
                //Button.Glyph = Icons.Cancel;
                Button.SetGlyph(file.Id, MessageContentState.Uploading);
                Button.Progress = (double)file.Remote.UploadedSize / size;
            }
            else if (file.Local.CanBeDownloaded && !file.Local.IsDownloadingCompleted)
            {
                //Button.Glyph = Icons.Download;
                Button.SetGlyph(file.Id, MessageContentState.Download);
                Button.Progress = 0;

                if (message.Delegate.CanBeDownloaded(voiceNote, file))
                {
                    _message.ClientService.DownloadFile(file.Id, 32);
                }
                else
                {
                    UpdateDuration();
                }
            }
            else
            {
                if (message.AreTheSame(message.PlaybackService.CurrentItem))
                {
                    if (message.PlaybackService.PlaybackState == MediaPlaybackState.Paused)
                    {
                        //Button.Glyph = Icons.Play;
                        Button.SetGlyph(file.Id, MessageContentState.Play);
                    }
                    else
                    {
                        //Button.Glyph = Icons.Pause;
                        Button.SetGlyph(file.Id, MessageContentState.Pause);
                    }

                    UpdatePosition(message.PlaybackService.Position, message.PlaybackService.Duration);

                    message.PlaybackService.PlaybackStateChanged += OnPlaybackStateChanged;
                    message.PlaybackService.PositionChanged += OnPositionChanged;
                }
                else
                {
                    //Button.Glyph = Icons.Play;
                    Button.SetGlyph(file.Id, MessageContentState.Play);
                    UpdateDuration();
                }

                Button.Progress = 1;
            }
        }

        public bool IsValid(MessageContent content, bool primary)
        {
            if (content is MessageVoiceNote)
            {
                return true;
            }
            else if (content is MessageText text && text.WebPage != null && !primary)
            {
                return text.WebPage.VoiceNote != null;
            }

            return false;
        }

        private VoiceNote GetContent(MessageContent content)
        {
            if (content is MessageVoiceNote voiceNote)
            {
                return voiceNote.VoiceNote;
            }
            else if (content is MessageText text && text.WebPage != null)
            {
                return text.WebPage.VoiceNote;
            }

            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var voiceNote = GetContent(_message?.Content);
            if (voiceNote == null)
            {
                return;
            }

            var file = voiceNote.Voice;
            if (file.Local.IsDownloadingActive)
            {
                _message.ClientService.CancelDownloadFile(file.Id);
            }
            else if (file.Remote.IsUploadingActive || _message.SendingState is MessageSendingStateFailed)
            {
                _message.ClientService.Send(new DeleteMessages(_message.ChatId, new[] { _message.Id }, true));
            }
            else if (file.Local.CanBeDownloaded && !file.Local.IsDownloadingActive && !file.Local.IsDownloadingCompleted)
            {
                _message.Delegate.PlayMessage(_message);
            }
            else
            {
                if (_message.AreTheSame(_message.PlaybackService.CurrentItem))
                {
                    if (_message.PlaybackService.PlaybackState == MediaPlaybackState.Paused)
                    {
                        _message.PlaybackService.Play();
                    }
                    else
                    {
                        _message.PlaybackService.Pause();
                    }
                }
                else
                {
                    _message.Delegate.PlayMessage(_message);
                }
            }
        }

        private void Recognize_Click(object sender, RoutedEventArgs e)
        {
            if (Recognize.IsChecked == true)
            {
                var voiceNote = GetContent(_message?.Content);
                if (voiceNote == null)
                {
                    return;
                }

                if (voiceNote.SpeechRecognitionResult == null)
                {
                    _message.ClientService.Send(new RecognizeSpeech(_message.ChatId, _message.Id));
                }
                else
                {
                    UpdateRecognitionResult(voiceNote.SpeechRecognitionResult);
                }
            }
            else if (RecognizedText != null)
            {
                RecognizedText.Visibility = Visibility.Collapsed;
            }
        }
    }
}
