import 'package:flutter/material.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int counter = 0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Home")),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text("Kliknięcia: $counter", style: const TextStyle(fontSize: 22)),
            const SizedBox(height: 12),
            ElevatedButton(
              onPressed: () {
                setState(() => counter++);
              },
              child: const Text("Kliknij mnie"),
            ),
          ],
        ),
      ),
    );
  }
}
