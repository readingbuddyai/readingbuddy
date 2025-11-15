import 'package:flutter/material.dart';
import 'dart:math' as math;

/// 숙련도 원형 차트 위젯
class MasteryCircularChart extends StatefulWidget {
  final double percentage; // 0-100
  final String label;
  final double size;
  final double strokeWidth;

  const MasteryCircularChart({
    super.key,
    required this.percentage,
    this.label = '숙련도',
    this.size = 140,
    this.strokeWidth = 12,
  });

  @override
  State<MasteryCircularChart> createState() => _MasteryCircularChartState();
}

class _MasteryCircularChartState extends State<MasteryCircularChart>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      duration: const Duration(milliseconds: 1500),
      vsync: this,
    );

    _animation = Tween<double>(begin: 0, end: widget.percentage).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic),
    );

    _controller.forward();
  }

  @override
  void didUpdateWidget(MasteryCircularChart oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.percentage != widget.percentage) {
      _animation = Tween<double>(
        begin: _animation.value,
        end: widget.percentage,
      ).animate(
        CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic),
      );
      _controller.forward(from: 0);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Color _getColorForPercentage(double percentage) {
    if (percentage >= 70) {
      return const Color(0xFF4CAF50); // 초록 - 높음
    } else if (percentage >= 30) {
      return const Color(0xFFFF9800); // 주황 - 보통
    } else {
      return const Color(0xFFF44336); // 빨강 - 낮음
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return AnimatedBuilder(
      animation: _animation,
      builder: (context, child) {
        final currentValue = _animation.value;
        final color = _getColorForPercentage(currentValue);

        return SizedBox(
          width: widget.size,
          height: widget.size,
          child: Stack(
            alignment: Alignment.center,
            children: [
              // 배경 원
              CustomPaint(
                size: Size(widget.size, widget.size),
                painter: _CircleBackgroundPainter(
                  strokeWidth: widget.strokeWidth,
                  backgroundColor:
                      theme.colorScheme.onSurface.withOpacity(0.1),
                ),
              ),

              // 진행 원
              CustomPaint(
                size: Size(widget.size, widget.size),
                painter: _CircleProgressPainter(
                  percentage: currentValue,
                  strokeWidth: widget.strokeWidth,
                  color: color,
                ),
              ),

              // 중앙 텍스트
              Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '${currentValue.toInt()}%',
                    style: theme.textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: color,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    widget.label,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurface.withOpacity(0.6),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}

/// 원형 배경 페인터
class _CircleBackgroundPainter extends CustomPainter {
  final double strokeWidth;
  final Color backgroundColor;

  _CircleBackgroundPainter({
    required this.strokeWidth,
    required this.backgroundColor,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = backgroundColor
      ..strokeWidth = strokeWidth
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final center = Offset(size.width / 2, size.height / 2);
    final radius = (size.width - strokeWidth) / 2;

    canvas.drawCircle(center, radius, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

/// 원형 진행도 페인터
class _CircleProgressPainter extends CustomPainter {
  final double percentage;
  final double strokeWidth;
  final Color color;

  _CircleProgressPainter({
    required this.percentage,
    required this.strokeWidth,
    required this.color,
  });

  @override
  void paint(Canvas canvas, Size size) {
    if (percentage <= 0) return;

    final paint = Paint()
      ..color = color
      ..strokeWidth = strokeWidth
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final center = Offset(size.width / 2, size.height / 2);
    final radius = (size.width - strokeWidth) / 2;

    // 시작 각도 (-90도 = 12시 방향)
    const startAngle = -math.pi / 2;
    // 끝 각도 (percentage를 라디안으로 변환)
    final sweepAngle = 2 * math.pi * (percentage / 100);

    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      startAngle,
      sweepAngle,
      false,
      paint,
    );
  }

  @override
  bool shouldRepaint(covariant _CircleProgressPainter oldDelegate) {
    return oldDelegate.percentage != percentage || oldDelegate.color != color;
  }
}
