import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'package:hackathon_flutter/theme/colors.dart';
import '../widgets/challenge_list_widget.dart';
import 'package:smooth_page_indicator/smooth_page_indicator.dart';
import 'package:provider/provider.dart';
import '../providers/challenge_provider.dart';
import 'package:go_router/go_router.dart';
import '../services/token_storage.dart';


class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {

  final List<String> imgList = const [
    'assets/images/hackaton1.jpg',
    'assets/images/hackaton2.jpg',
    'assets/images/hackaton3.jpg',
  ];

  // sledzenie aktywnego indeksu dla wskaznika
  int activeIndex = 0;

  @override
  void initState() {
    super.initState();
    // Wywołaj ładowanie natychmiast, bez czekania na pierwszy frame
    Future.microtask(() {
      context.read<ChallengeProvider>().loadStateFromCache();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Witaj na stronie Hackathonu Goldman Sachs!'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: TextButton.icon(
              onPressed: () async {
                await TokenStorage.deleteToken();
                if (context.mounted) {
                  context.go('/');
                }
              },
              icon: const Icon(Icons.logout, color: Colors.white),
              label: const Text(
                'Wyloguj się',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            //slider jesli lista obrazkow not empty
            if (imgList.isNotEmpty)
            
              Column(
                children: [
                  CarouselSlider(
                    options: CarouselOptions(
                      autoPlay: true,
                      aspectRatio: 16 / 9,
                      viewportFraction: 0.75,
                      // Powiększamy środkowy slajd
                      enlargeCenterPage: true,
                          
                      // aktywny indeks przy zmianie strony
                      onPageChanged: (index, reason) {
                        setState(() {
                          activeIndex = index;
                        });
                      },
                    ),
                    items: imgList.map((item) {
                      return Container(
                        // Przywracamy mały margines między slajdami
                        margin: const EdgeInsets.symmetric(horizontal: 5.0),
                        width: double.infinity,
                        child: ClipRRect(
                          borderRadius: const BorderRadius.all(Radius.circular(10.0)),
                          child: Image.asset(
                            item,
                            fit: BoxFit.cover,
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                  
                  const SizedBox(height: 12), 
                  AnimatedSmoothIndicator(
                    activeIndex: activeIndex,
                    count: imgList.length,
                    effect: WormEffect( 
                      dotHeight: 10,
                      dotWidth: 10,
                      activeDotColor: Theme.of(context).colorScheme.primary, 
                      dotColor: Colors.grey.shade400,
                    ),
                  ),
                ],
              ),

            // lista wyzwan
            const Padding(
              padding: EdgeInsets.fromLTRB(16.0, 24.0, 16.0, 8.0),
              child: Text(
                'Dostępne Wyzwania',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              ),
            ),
            const Divider(height: 1, indent: 16, endIndent: 16),

            
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              child: ChallengeListWidget(),
            ),
          ],
        ),
      ),
    );
  }
}