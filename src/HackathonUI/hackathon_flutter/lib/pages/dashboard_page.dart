// lib/dashboard_page.dart
import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import '../widgets/challenge_list_widget.dart'; 

class DashboardPage extends StatelessWidget {
  const DashboardPage({super.key});

  final List<String> imgList =  const [
    'assets/images/hackaton1.jpg',
    'assets/images/hackaton2.jpg',
    'assets/images/hackaton3.jpg', 
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Align(
          alignment: Alignment.center,
          child: Text('Witaj na stronie Hackathonu Golman Sachs!'),
        ),
      ),
      
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // --- SLIDER ZE ZDJĘCIAMI (WIDGET GS) ---
            if (imgList.isNotEmpty) 
              CarouselSlider(
                options: CarouselOptions(
                  autoPlay: true, // Automatyczne przewijanie
                  aspectRatio: 16/9, // Proporcje (szerszy niż wyższy)
                  viewportFraction: 1.0, //jeden obraz na raz
                  enlargeCenterPage: false, //
                ),
                items: imgList.map((item) => Container(
                  margin: const EdgeInsets.all(5.0),
                  child: ClipRRect(
                    borderRadius: const BorderRadius.all(Radius.circular(10.0)),
                    child: Image.asset(
                      item,
                      fit: BoxFit.cover,
                      width: 1000.0,
                    ),
                  ),
                )).toList(),
              ),

            // --- DIVIDER I NAGŁÓWEK LISTY ---
            const Padding(
              padding: EdgeInsets.fromLTRB(16.0, 24.0, 16.0, 8.0),
              child: Text(
                'Dostępne Wyzwania',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              ),
            ),
            const Divider(height: 1, indent: 16, endIndent: 16),

            // --- SEKCJA Z LISTĄ WYZWAŃ (LAZY LOADING) ---
            // Używamy "opakowania" Expanded, aby lista zajęła resztę miejsca
            // Ale ponieważ jesteśmy w SingleChildScrollView, lepiej dać jej stałą wysokość
            // Lepsze podejście: Używamy 'shrinkWrap' i 'physics' w liście
            // Poniżej wstawimy nasz widget z lazy-loadingiem
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              // Ten widget stworzymy w następnym kroku
              child: ChallengeListWidget(),
            ),
          ],
        ),
      ),
    );
  }
}