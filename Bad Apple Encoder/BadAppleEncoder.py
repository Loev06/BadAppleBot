import cv2 # pip install opencv-python
import numpy as np # pip install numpy

# A very crude implementation of a video encoder, by Loev06
# Captures the pixels of given width and height for a given amount of time and fps,
# And converts it to several encoded data structures.

# This program contains 3 methods of encoding the video data. The second method is the
# one producing the data used in the C# decoder.

goal_fps = 30
goal_width = 8
goal_height = 8
max_time = 121


###########################
#      Video-capture      #
###########################

def read_video():
	# Read the video
	video = cv2.VideoCapture("BadApple.mp4")
	# Get the number of frames
	video_frames = int(video.get(cv2.CAP_PROP_FRAME_COUNT))
	video_fps = int(video.get(cv2.CAP_PROP_FPS))

	# Calculate max frame number
	if max_time == 0:
		max_frames = video_frames
	else:
		max_frames = int(min(video_frames, video_fps * max_time))

	# Calculate the number of frames to skip between captures
	step_frames = video_fps / goal_fps

	frames = []
	# ActualFrame might be a floating point number, if goal_fps is not a factor of video_fps.
	actualFrame = i = 0
	while True:
		actualFrame += step_frames
		i = int(actualFrame)

		if i >= max_frames:
			break

		video.set(cv2.CAP_PROP_POS_FRAMES, i)
		ret, frame = video.read()
		# Resize the frame
		frame = cv2.resize(frame, (goal_width, goal_height))
		# Convert the frame to grayscale
		frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

		# Convert the frame to a ulong
		ulong = 0
		for y in range(goal_height):
			for x in range(goal_width):
				if frame[y][x] > 127:
					# calculate ulong coordinate of the pixel,
					# making the video right side up assuming the chess board is not flipped
					ulong += 1 << 63 - (y * goal_width + goal_width - 1 - x)
		
		frames.append(ulong)
	
	# Write the frames to a file to cache the data for later use
	with open(rawDataFileName, "w") as f:
		for frame in frames:
			f.write(str(frame) + "\n")
	return frames

def read_file():
	frames = []
	with open(rawDataFileName, "r") as f:
		for line in f:
			frames.append(int(line))
	return frames


###########################
#      Program start      #
###########################

rawDataFileName = f"RawData\\Data{goal_fps}_{max_time}.txt"
try:
	print("Trying to read raw data from file..")
	frames = read_file()
except FileNotFoundError:
	print("Raw data file not found..")
	print(f"Reading video at {goal_fps} fps for {max_time} seconds")
	frames = read_video()

print(f"fps: {goal_fps}\ttime: {max_time if max_time > 0 else 'unlimited'} seconds\n")

# Calculate difference between frames
diff_frames = []
diff_count = []
for i in range(len(frames)-1):
	diff_frames.append(frames[i] ^ frames[i+1])
	diff_count.append(bin(diff_frames[i]).count("1"))

# Write the amount of pixels that change per frame to a file (for debugging)
with open("DebugData\\DiffData.txt", "w") as f:
	for frame in diff_count:
		f.write(str(frame) + "\n")

print(f"total frames:\t\t{len(frames) + 1}")
print(f"total pixel changes:\t{np.sum(diff_count)}\n")


##########################
#        Method 1        #
##########################

# Calculates the mask, the startIndex and the length of the first sequence of set bits in an ulong.
def find_first_seq(frame):
	if frame == 0:
		return (0, 0, 0)

	lsb = frame & -frame
	start = int(np.log2(lsb))
	length = 1
	while (lsb | lsb << 1) & frame != lsb & frame and start + length < 64 and length < 15:
		lsb |= lsb << 1
		length += 1

	return (lsb, start, length) # mask, startIndex, length

# Finds all sequences of set bits within an ulong. Returned list consists of (startIndex, length) pairs.
def get_seqs(frame):
	seqs = []
	while frame != 0:
		seq = find_first_seq(frame)
		seqs.append(seq[1:])
		frame ^= seq[0]
	return seqs

# Find the sequences of changed bits per frame.
diff_seqs = []
for i in diff_frames:
	diff_seqs.append(get_seqs(i))

# Write the found sequences to a debug file.
with open("DebugData\\DiffSeqs.txt", "w") as f:
	for frame in diff_seqs:
		f.write(str(frame) + "\n")

count = 0
for i in diff_seqs:
	count += len(i)

# Print the amount of tokens the program would use if the diffSeqs data would be compressed into ulongs
print(f"diff seqs tokens:\t\t{round(count // 6.4)}")


##########################
#        Method 2        #
##########################

# Calculate the sequence of wait times in frames for each pixel to switch color throughout the video.
pixel_diffs = []
for i in range(64):
	pixel_diffs.append([0])
	mask = 1 << i
	for frame in diff_frames:
		if frame & mask == 0:
			pixel_diffs[i][-1] += 1
		else:
			pixel_diffs[i].append(1)

# Write these wait times to a debug file. From this you can clearly see the
# widely differing amount of times each pixel has to switch color.
with open("DebugData\\PixelDiffs.txt", "w") as f:
	for pixel in pixel_diffs:
		f.write(' '.join(map(str, pixel)) + "\n")

# Convert a list of wait times to a binary representation of one of the "pixel blocks" as described in MyBot.cs
# Add this data word-by word in reverse order, because the string gets reversed later on.
def generate_pixel_data(bit_count, raw):
	pixel_data = ""
	for i in raw:
		while i >= 1 << bit_count:
			# Add bit_count zeroes if the wait time >= 2^bit_count
			pixel_data = "0" * bit_count + pixel_data
			i -= (1 << bit_count) - 1
		# Add the remaining wait time converted to a binary string, removing the "0b" prefix
		pixel_data = bin(i)[2:].zfill(bit_count) + pixel_data
	return pixel_data

total = ""
for pixel in pixel_diffs:
	# Calculate the lengths of the pixel data for differing word sizes, and select the shortest one.
	pixel_datas = [generate_pixel_data(i, pixel) for i in range(2, 16)]

	# The following loop could be more efficient, but due to a small bug in the original (not cleaned) encoder,
	# the output became slightly different. The following loop simulates the old behaviour, while being readable
	# and fixing the bug that would not allow certain video times alltogether :)
	bit_count = 0
	pixel_data = pixel_datas[0]
	for i in range(2, 16):
		if len(pixel_datas[i - 2]) <= len(pixel_data):
			bit_count = i
			pixel_data = pixel_datas[i - 2]

	# Word size range is from {min_word_size} to (including) {min_word_size + 2^word_size_size - 1}
	min_word_size = 4
	word_size_size = 3 # Amount of bits used to encode word size
	
	total = pixel_data + bin(bit_count - min_word_size)[2:].zfill(word_size_size) + total # Append (in reverse order), the word size, and the actual data of the pixel
	total = "0" * bit_count + total[bit_count:] # Replace last word of the block by bit_count zeroes, so the token-optimized C# script continues the decoding loop properly.

	# I used a breakpoint to determine if the used word size would fit in the hardcoded range (4 to 4 + 2^3-1 = 4 to 11)
	# If not, I manually adjusted the range (some video_time-fps combinations allowed for a word size range from 4 to 7 for example)
	if bit_count < min_word_size or bit_count >= min_word_size + 1 << word_size_size or "b" in total:
		print("Error")

# Print the amount of tokens this method would use up. This ended up being the most efficient in most cases, so the C# bot uses this data.
print(f"pixel diffs tokens:\t\t{len(total) / 64}")

# Write (In reverse order) the blocks of the pixel datas converted to hex ulongs, ready to be copy-pasted into the C# script.
with open("PixelData.txt", "w") as f:
	for i in range(len(total), 0, -64):
		f.write(hex(int(total[max(0, i-64):i], 2)) + ",")
	f.write("0") # Extra index to prevent overflow when reading


##########################
#        Method 3        #
##########################
# A third method I tried to compress the data, similar to the first sequence method, but taking a sequence of alway neigbouring pixels.
# This ended up being less efficient in all cases, but I decided to leave it in.
spiral = [
	 0,  1,  2,  3,  4,  5,  6,  7,
	27, 28, 29, 30, 31, 32, 33,  8,
	26, 47, 48, 49, 50, 51, 34,  9,
	25, 46, 59, 60, 61, 52, 35, 10,
	24, 45, 58, 63, 62, 53, 36, 11,
	23, 44, 57, 56, 55, 54, 37, 12,
	22, 43, 42, 41, 40, 39, 38, 13,
	21, 20, 19, 18, 17, 16, 15, 14
]

order = [None] * 64
for i in range(64):
	order[spiral[i]] = i

def get_frame_data(frame, bit_count):
	frame_data = ""
	count = 0
	color = frame >> order[0] & 1
	for i in range(64):
		if frame >> order[i] & 1 == color:
			count += 1
			if count == (1 << bit_count):
				frame_data += "0" * bit_count
				count = 1
		else:
			frame_data += bin(count)[2:].zfill(bit_count)
			count = 1
			color = 1 - color
	frame_data += bin(count)[2:].zfill(bit_count)
	return frame_data

total_length = 0
for frame in frames:
	total_length += min([len(get_frame_data(frame, i)) for i in range(2, 8)])

print(f"spiral intraframe tokens:\t{total_length // 64}")