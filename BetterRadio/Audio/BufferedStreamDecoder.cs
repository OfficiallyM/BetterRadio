using BetterRadio.Utilities;
using System;

namespace BetterRadio.Audio
{
	// Base for decoders that produce audio a block at a time (one AAC frame, one Ogg packet, one FLAC frame).
	// The output format is locked to whatever the first block decodes as, because the player creates its clip from that
	// and cannot change it mid-stream. Later blocks in a different format (an Ogg chain switching encoder settings at a
	// track change, for instance) are remixed and resampled to match rather than played back at the wrong speed.
	internal abstract class BufferedStreamDecoder : IStreamDecoder
	{
		// More than stereo is pointless in a car radio and Unity's handling of surround layouts varies, so it is folded down.
		private const int MaxOutputChannels = 2;

		private float[] _pending = new float[16384];
		private int _pendingStart;
		private int _pendingEnd;
		private bool _ended;

		private int _sampleRate;
		private int _channels;

		// Resampler state, only used once a block arrives at a different rate from the locked one.
		private int _resampleFromRate;
		private double _resamplePosition;
		private float[] _previousFrame;
		private float[] _currentFrame;

		public int SampleRate => _sampleRate;
		public int Channels => _channels;

		// Decodes the next block and hands it to Emit. Returns false once the stream has ended.
		// Emitting nothing and returning true is fine, for header packets and skipped corrupt frames.
		protected abstract bool DecodeNextBlock();

		public int ReadSamples(float[] buffer, int offset, int count)
		{
			while (!_ended && (_channels == 0 || _pendingEnd - _pendingStart < count))
			{
				if (!DecodeNextBlock())
					_ended = true;
			}

			int available = _pendingEnd - _pendingStart;
			int toCopy = Math.Min(count, available);

			// Only whole frames are handed out, so the caller can never see the channels swap.
			if (_channels > 0)
				toCopy -= toCopy % _channels;

			Array.Copy(_pending, _pendingStart, buffer, offset, toCopy);
			_pendingStart += toCopy;
			return toCopy;
		}

		// Queues interleaved samples. leftIndex and rightIndex say which source channels are front left and right,
		// since codecs disagree on surround channel order.
		protected void Emit(float[] samples, int offset, int frames, int sampleRate, int channels, int leftIndex = 0, int rightIndex = 1)
		{
			if (frames <= 0 || channels <= 0 || sampleRate <= 0) return;

			if (_channels == 0)
			{
				_channels = Math.Min(channels, MaxOutputChannels);
				_sampleRate = sampleRate;
			}

			if (sampleRate != _sampleRate && sampleRate != _resampleFromRate)
			{
				Logging.LogDebug($"Stream format changed to {sampleRate} Hz, resampling to {_sampleRate} Hz.");
				_resampleFromRate = sampleRate;
				_resamplePosition = 0;
				_previousFrame = null;
			}

			if (sampleRate == _sampleRate)
			{
				EnsureCapacity(frames * _channels);
				for (int frame = 0; frame < frames; frame++)
				{
					MapFrame(samples, offset + frame * channels, channels, leftIndex, rightIndex, _pending, _pendingEnd);
					_pendingEnd += _channels;
				}
				return;
			}

			Resample(samples, offset, frames, sampleRate, channels, leftIndex, rightIndex);
		}

		// Linear interpolation is crude, but this only runs in the rare case a stream changes rate partway through.
		private void Resample(float[] samples, int offset, int frames, int sampleRate, int channels, int leftIndex, int rightIndex)
		{
			double step = sampleRate / (double)_sampleRate;
			EnsureCapacity((int)(frames / step + 2) * _channels);

			if (_currentFrame == null || _currentFrame.Length != _channels)
				_currentFrame = new float[_channels];

			for (int frame = 0; frame < frames; frame++)
			{
				MapFrame(samples, offset + frame * channels, channels, leftIndex, rightIndex, _currentFrame, 0);

				if (_previousFrame == null)
				{
					_previousFrame = (float[])_currentFrame.Clone();
					continue;
				}

				while (_resamplePosition < 1.0)
				{
					EnsureCapacity(_channels);
					float t = (float)_resamplePosition;
					for (int c = 0; c < _channels; c++)
						_pending[_pendingEnd + c] = _previousFrame[c] + (_currentFrame[c] - _previousFrame[c]) * t;

					_pendingEnd += _channels;
					_resamplePosition += step;
				}

				_resamplePosition -= 1.0;
				Array.Copy(_currentFrame, _previousFrame, _channels);
			}
		}

		private void MapFrame(float[] source, int sourceIndex, int sourceChannels, int leftIndex, int rightIndex, float[] target, int targetIndex)
		{
			if (sourceChannels == _channels && sourceChannels <= 2)
			{
				for (int c = 0; c < sourceChannels; c++)
					target[targetIndex + c] = source[sourceIndex + c];
				return;
			}

			float left;
			float right;

			if (sourceChannels == 1)
			{
				left = right = source[sourceIndex];
			}
			else
			{
				left = source[sourceIndex + Math.Min(leftIndex, sourceChannels - 1)];
				right = source[sourceIndex + Math.Min(rightIndex, sourceChannels - 1)];
			}

			if (_channels == 1)
			{
				target[targetIndex] = (left + right) * 0.5f;
			}
			else
			{
				target[targetIndex] = left;
				target[targetIndex + 1] = right;
			}
		}

		private void EnsureCapacity(int extra)
		{
			if (_pendingEnd + extra <= _pending.Length) return;

			int unread = _pendingEnd - _pendingStart;
			float[] target = unread + extra <= _pending.Length ? _pending : new float[Math.Max(unread + extra, _pending.Length * 2)];
			Array.Copy(_pending, _pendingStart, target, 0, unread);
			_pending = target;
			_pendingStart = 0;
			_pendingEnd = unread;
		}

		public virtual void Dispose()
		{
		}
	}
}
